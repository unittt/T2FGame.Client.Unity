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
        public event Action<CmdInfo, int, SendResult> OnSendFailed;

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

        /// <summary>
        /// 检查是否可以发送，不可发送时抛出异常
        /// </summary>
        private void ThrowIfCannotSend()
        {
            if (_disposed)
                throw new PiscesSendException(SendResult.ClientClosed);

            if (!IsConnected)
                throw new PiscesSendException(SendResult.NotConnected);
        }

        /// <summary>
        /// 尝试验证发送状态
        /// </summary>
        /// <returns>验证失败时返回失败原因，成功时返回 null</returns>
        private SendResult? TryValidateSendState()
        {
            if (_disposed)
                return SendResult.ClientClosed;

            if (!IsConnected)
                return SendResult.NotConnected;

            return null;
        }

        public async UniTask SendAsync(ExternalMessage message, CancellationToken cancellationToken = default)
        {
            ThrowIfCannotSend();

            if (message == null)
                throw new PiscesSendException(SendResult.InvalidMessage);

            try
            {
                var packet = PacketCodec.Encode(message);
                if (!_channel.Send(packet))
                {
                    throw new PiscesSendException(SendResult.ChannelError, message.CmdMerge, message.MsgId);
                }

                await UniTask.CompletedTask;
            }
            catch (PiscesSendException)
            {
                throw;
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
            ThrowIfCannotSend();

            if (command == null)
                throw new PiscesSendException(SendResult.InvalidMessage);

            // 非业务消息不需要等待响应，直接发送
            if (command.MessageType != MessageType.Business)
            {
                var result = SendInternal(command, out var pktLen);
                if (result != SendResult.Success)
                {
                    throw new PiscesSendException(result, command.CmdInfo, command.MsgId);
                }

                _statistics.RecordSend(pktLen, command);
                ReferencePool<RequestCommand>.Despawn(command);
                return null;
            }

            // 业务请求去重检查
            if (!TryLockRoute(command.CmdInfo))
            {
                throw new PiscesSendException(SendResult.RequestLocked, command.CmdInfo, command.MsgId);
            }

            // 创建等待响应的信息
            var tcs = new UniTaskCompletionSource<ResponseMessage>();
            var pendingInfo = new PendingRequestInfo
            {
                Tcs = tcs,
                CreatedTicks = Stopwatch.GetTimestamp(),
                CmdInfo = command.CmdInfo,
                MsgId = command.MsgId
            };

            // 添加到等待列表
            if (!_pendingRequests.TryAdd(command.MsgId, pendingInfo))
            {
                UnlockRoute(command.CmdInfo);
                throw new PiscesSendException(SendResult.DuplicateMsgId, command.CmdInfo, command.MsgId);
            }

            try
            {
                // 发送请求
                var result = SendInternal(command, out var packetLength);
                if (result != SendResult.Success)
                {
                    throw new PiscesSendException(result, command.CmdInfo, command.MsgId);
                }

                // 记录统计
                _statistics.RecordSend(packetLength, command);

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
                throw new PiscesSendException(SendResult.Timeout, command.CmdInfo, command.MsgId);
            }
            finally
            {
                _pendingRequests.TryRemove(command.MsgId, out _);

                // 解锁路由
                UnlockRoute(command.CmdInfo);

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
                return SendResult.InvalidMessage;

            var validateResult = TryValidateSendState();
            if (validateResult.HasValue)
            {
                NotifySendFailed(command, validateResult.Value);
                ReferencePool<RequestCommand>.Despawn(command);
                return validateResult.Value;
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

                var result = SendInternal(command, out var packetLength);
                if (result != SendResult.Success)
                {
                    NotifySendFailed(command, result);
                    return result;
                }

                // 记录统计 (心跳消息特殊处理)
                if (command.MessageType == MessageType.Heartbeat)
                {
                    _statistics.RecordHeartbeatSend();
                }

                _statistics.RecordSend(packetLength, command);

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
            GameLogger.LogWarning($"[GameClient] 发送失败: {command.CmdInfo.ToString()}, MsgId={command.MsgId}, 原因={result}");
            _statistics.RecordSendFailed();
            OnSendFailed?.Invoke(command.CmdInfo, command.MsgId, result);
        }

        private ExternalMessage CreateExternalMessage(RequestCommand command)
        {
            var message = ReferencePool<ExternalMessage>.Spawn();
            message.MessageType = command.MessageType;
            message.CmdMerge = command.CmdInfo;
            message.MsgId = command.MsgId;
            message.Data = command.Data;
            return message;
        }

        /// <summary>
        /// 内部发送方法，统一消息编码和发送逻辑
        /// </summary>
        /// <param name="command">请求命令</param>
        /// <param name="packetLength">发送成功时的数据包长度</param>
        /// <returns>发送结果</returns>
        private SendResult SendInternal(RequestCommand command, out int packetLength)
        {
            var message = CreateExternalMessage(command);
            var packet = PacketCodec.Encode(message);
            ReferencePool<ExternalMessage>.Despawn(message);

            if (!_channel.Send(packet))
            {
                packetLength = 0;
                return SendResult.ChannelError;
            }

            packetLength = packet.Length;
            return SendResult.Success;
        }

        #region 响应消息处理
        private void OnChannelReceiveMessage(IProtocolChannel channel, ArraySegment<byte> data)
        {
            if (_disposed || data.Count == 0)
                return;

            try
            {
                // 写入缓冲区并尝试解析完整数据包
                _receiveBuffer.Write(data);
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

            switch (message.MessageType)
            {
                case MessageType.Heartbeat:
                    ProcessHeartbeat();
                    break;
                case MessageType.TimeSync:
                    ProcessTimeSync(message);
                    break;
                case MessageType.Disconnect:
                    ProcessDisconnectNotify(message);
                    break;
                default:
                    ProcessBusinessMessage(message);
                    break;
            }
        }

        private void ProcessHeartbeat()
        {
            _heartbeatTimeoutCount = 0;
            _statistics.RecordHeartbeatReceive();
            GameLogger.LogVerbose("[GameClient] 收到心跳响应");
        }

        private void ProcessTimeSync(ExternalMessage message)
        {
            var timeSyncMsg = TimeSyncMessage.Parser.ParseFrom(message.Data);
            TimeUtils.UpdateSync(timeSyncMsg.ClientTime, timeSyncMsg.ServerTime);
        }

        private void ProcessBusinessMessage(ExternalMessage message)
        {
            // 记录接收统计
            _statistics.RecordReceive(message);

            // 创建响应消息
            var response = ReferencePool<ResponseMessage>.Spawn();
            response.Initialize(message);

            // 尝试匹配等待的请求
            if (_pendingRequests.TryRemove(message.MsgId, out var pendingInfo))
            {
                // 匹配到请求，设置响应结果
                pendingInfo.Tcs?.TrySetResult(response);
            }

            // 触发消息接收事件
            // 警告：订阅者需要在回调中同步处理或复制所需数据
            OnMessageReceived?.Invoke(message);

            // 统一释放 response（主线程同步执行，调用者已处理完毕）
            ReferencePool<ResponseMessage>.Despawn(response);
        }
        #endregion

        private void ProcessDisconnectNotify(ExternalMessage message)
        {
            // 解析断线通知
            var disconnectNotify = DisconnectNotify.Parser.ParseFrom(message.Data);

            GameLogger.LogWarning($"[GameClient] 收到服务器断线通知: Reason={disconnectNotify.Reason}, Message={disconnectNotify.Message}");

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
