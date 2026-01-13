using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Channel;
using Pisces.Client.Network.Core;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Network
{
    /// <summary>
    /// GameClient - 消息收发部分
    /// </summary>
    public partial class GameClient
    {
        // 消息缓冲区
        private PacketBuffer _receiveBuffer;

        // 限流器
        private RateLimiter _rateLimiter;

        /// <summary>
        /// 收到消息事件
        /// </summary>
        public event Action<ExternalMessage> OnMessageReceived;

        /// <summary>
        /// 消息发送失败事件
        /// 参数：CmdMerge, MsgId, 失败原因
        /// </summary>
        public event Action<int, int, SendResult> OnSendFailed;

        /// <summary>
        /// 初始化消息模块
        /// </summary>
        private void InitMessaging()
        {
            _receiveBuffer = new PacketBuffer(
                _options.PacketBufferInitialSize,
                _options.PacketBufferShrinkThreshold
            );

            // 初始化限流器
            if (_options.EnableRateLimit && _options.MaxSendRate > 0)
            {
                _rateLimiter = new RateLimiter(_options.MaxBurstSize, _options.MaxSendRate);
                GameLogger.LogDebug($"[GameClient] 限流器已启用: 速率={_options.MaxSendRate}/s, 突发={_options.MaxBurstSize}");
            }
        }

        /// <summary>
        /// 释放消息模块资源
        /// </summary>
        private void DisposeMessaging()
        {
            _receiveBuffer?.Clear();
            _receiveBuffer = null;
        }

        public async UniTask SendAsync(
            ExternalMessage message,
            CancellationToken cancellationToken = default
        )
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (!IsConnected)
                throw new InvalidOperationException("未连接");

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                var packet = PacketCodec.Encode(message);
                if (!_channel.Send(packet))
                {
                    throw new InvalidOperationException("通道发送失败");
                }

                await UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[GameClient] 发送失败: {ex.Message}");
                OnError?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        /// <param name="command">请求命令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息</returns>
        public async UniTask<ResponseMessage> RequestAsync(RequestCommand command, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (!IsConnected)
                throw new InvalidOperationException("未连接");

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            PendingRequestInfo pendingInfo = null;

            // 只有业务消息才需要等待响应
            if (command.MessageType == MessageType.Business)
            {
                var tcs = new UniTaskCompletionSource<ResponseMessage>();
                pendingInfo = new PendingRequestInfo
                {
                    Tcs = tcs,
                    CreatedTicks = Stopwatch.GetTimestamp(),
                    CmdMerge = command.CmdMerge,
                    MsgId = command.MsgId
                };

                if (!_pendingRequests.TryAdd(command.MsgId, pendingInfo))
                {
                    throw new InvalidOperationException($"重复的 MsgId: {command.MsgId}");
                }
            }

            try
            {
                // 发送请求
                var message = CreateExternalMessage(command);
                var packet = PacketCodec.Encode(message);
                ReferencePool<ExternalMessage>.Despawn(message); // 编码后立即归还

                if (!_channel.Send(packet))
                {
                    throw new InvalidOperationException("通道发送失败");
                }

                // 记录统计
                _statistics.RecordSend(packet.Length, command);

                // 非业务消息不需要等待响应
                if (pendingInfo == null)
                {
                    return null;
                }

                // 等待响应（带超时）
                using var timeoutCts = new CancellationTokenSource(_options.RequestTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCts.Token
                );

                var response = await pendingInfo.Tcs.Task.AttachExternalCancellation(linkedCts.Token);
                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Request timeout after {_options.RequestTimeoutMs}ms (MsgId: {command.MsgId})"
                );
            }
            finally
            {
                _pendingRequests.TryRemove(command.MsgId, out _);

                // 归还到对象池
                ReferencePool<RequestCommand>.Despawn(command);
            }
        }

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        /// <returns>发送结果</returns>
        public SendResult SendRequest(RequestCommand command)
        {
            if (command == null)
            {
                return SendResult.InvalidMessage;
            }

            if (_disposed)
            {
                NotifySendFailed(command, SendResult.ClientClosed);
                ReferencePool<RequestCommand>.Despawn(command);
                return SendResult.ClientClosed;
            }

            if (!IsConnected)
            {
                NotifySendFailed(command, SendResult.NotConnected);
                ReferencePool<RequestCommand>.Despawn(command);
                return SendResult.NotConnected;
            }

            try
            {
                // 流量控制（心跳消息豁免）
                if (_rateLimiter != null && command.MessageType == MessageType.Business)
                {
                    if (!_rateLimiter.TryAcquire())
                    {
                        _statistics.RecordRateLimited();
                        NotifySendFailed(command, SendResult.RateLimited);
                        return SendResult.RateLimited;
                    }
                }

                var message = CreateExternalMessage(command);
                var packet = PacketCodec.Encode(message);
                ReferencePool<ExternalMessage>.Despawn(message); // 编码后立即归还

                if (!_channel.Send(packet))
                {
                    NotifySendFailed(command, SendResult.ChannelError);
                    return SendResult.ChannelError;
                }

                // 记录统计 (心跳消息特殊处理)
                if (command.MessageType == MessageType.Heartbeat)
                {
                    _statistics.RecordHeartbeatSend();
                }

                _statistics.RecordSend(packet.Length, command);

                return SendResult.Success;
            }
            finally
            {
                ReferencePool<RequestCommand>.Despawn(command);
            }
        }

        /// <summary>
        /// 通知发送失败
        /// </summary>
        private void NotifySendFailed(RequestCommand command, SendResult result)
        {
            GameLogger.LogWarning(
                $"[GameClient] 发送失败: {CmdKit.ToString(command.CmdMerge)}, MsgId={command.MsgId}, 原因={result}"
            );

            _statistics.RecordSendFailed();
            OnSendFailed?.Invoke(command.CmdMerge, command.MsgId, result);
        }

        private ExternalMessage CreateExternalMessage(RequestCommand command)
        {
            var message = ReferencePool<ExternalMessage>.Spawn();
            message.MessageType = command.MessageType;
            message.CmdMerge = command.CmdMerge;
            message.MsgId = command.MsgId;
            message.Data = command.Data;
            return message;
        }

        private void OnChannelReceiveMessage(IProtocolChannel channel, byte[] data)
        {
            if (_disposed || data == null || data.Length == 0)
                return;

            try
            {
                // 写入缓冲区并尝试解析完整数据包
                _receiveBuffer.Write(data, 0, data.Length);
                var messages = _receiveBuffer.ReadPackets();

                foreach (var message in messages)
                {
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[GameClient] 处理消息失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        private void ProcessMessage(ExternalMessage message)
        {
            if (message == null) return;

            // 判断是心跳响应还是业务消息
            if (message.MessageType == MessageType.Heartbeat)
            {
                // 心跳响应，重置超时计数
                _heartbeatTimeoutCount = 0;
                _statistics.RecordHeartbeatReceive();
                GameLogger.LogVerbose("[GameClient] 收到心跳响应");
                return;
            }

            // 记录接收统计
            _statistics.RecordReceive(message);

            // 时间同步
            if (message.MessageType == MessageType.TimeSync)
            {
                var timeSyncMsg = TimeSyncMessage.Parser.ParseFrom(message.Data);
                TimeUtils.UpdateSync(timeSyncMsg.ClientTime, timeSyncMsg.ServerTime);
                return;
            }

            // 断线通知
            if (message.MessageType == MessageType.Disconnect)
            {
                ProcessDisconnectNotify(message);
                return;
            }

            // 创建响应消息
            var response = ReferencePool<ResponseMessage>.Spawn();
            response.Initialize(message);

            // 尝试匹配等待的请求
            if (_pendingRequests.TryRemove(message.MsgId, out var pendingInfo))
            {
                // 匹配到请求，设置响应结果
                pendingInfo.Tcs?.TrySetResult(response);
            }

            // 触发消息接收事件（所有业务消息都触发，包括请求响应和服务器推送）
            OnMessageReceived?.Invoke(message);

            // 归还响应消息
            ReferencePool<ResponseMessage>.Despawn(response);
        }

        private void ProcessDisconnectNotify(ExternalMessage message)
        {
            // 解析断线通知
            var disconnectNotify = DisconnectNotify.Parser.ParseFrom(message.Data);

            GameLogger.LogWarning(
                $"[GameClient] 收到服务器断线通知: Reason={disconnectNotify.Reason}, Message={disconnectNotify.Message}"
            );

            // 触发断线通知事件，让上层处理（如显示提示 UI）
            OnDisconnectNotify?.Invoke(disconnectNotify);

            // 根据断线原因决定是否允许自动重连
            if (!IsReconnectAllowed(disconnectNotify.Reason))
            {
                // 禁止重连的情况，直接关闭客户端
                GameLogger.LogDebug($"[GameClient] 断线原因 {disconnectNotify.Reason} 不允许重连，关闭客户端");
                Close();
            }
            else
            {
                // 允许重连的情况，正常断开（会触发自动重连）
                _channel?.Disconnect();
            }
        }
    }
}
