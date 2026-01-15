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
    /// GameClient - 消息收发逻辑部分
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
        public event Action<ResponseMessage> OnMessageReceived;

        /// <summary>
        /// 消息发送失败事件
        /// 参数：CmdInfo, MsgId, 失败原因
        /// </summary>
        public event Action<CmdInfo, int, PiscesClientCode> OnSendFailed;


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
        ///  发送请求，不等待响应
        /// </summary>
        /// <param name="command"></param>
        public void Send(RequestCommand command)
        {
            RequestAsyncCore(command,false).Forget();
        }

        /// <summary>
        /// 发送请求，等待响应
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask<ResponseMessage> RequestAsync(RequestCommand command,  CancellationToken cancellationToken = default)
        {
            return await RequestAsyncCore(command, true, cancellationToken);
        }

        /// <summary>
        ///  发送请求核心方法
        /// </summary>
        /// <param name="command">请求指令</param>
        /// <param name="waitForResponse">是否等待响应</param>
        /// <param name="cancellationToken">取消Token</param>
        private async UniTask<ResponseMessage> RequestAsyncCore(RequestCommand command, bool waitForResponse = true, CancellationToken cancellationToken = default)
        {
            var routeLocked = false;
            var pendingAdded = false;
            ResponseMessage response = null;

            try
            {
                // 1. 基础校验
                if (command == null) PiscesClientCode.InvalidRequestCommand.ThrowIfNotSuccess();
                if (_disposed || _isClosed) PiscesClientCode.ClientClosed.ThrowIfNotSuccess();
                if (!IsConnected) PiscesClientCode.NotConnected.ThrowIfNotSuccess();

                PendingRequestInfo pendingInfo = null;

                // 2. 业务消息加锁和限流
                if (command.MessageType == MessageType.Business)
                {
                    if (!TryLockRoute(command.CmdInfo))
                    {
                        PiscesClientCode.RequestLocked.ThrowIfNotSuccess(command);
                    }

                    routeLocked = true;

                    if (_rateLimiter != null && !_rateLimiter.TryAcquire())
                    {
                        _statistics.RecordRateLimited();
                        PiscesClientCode.RateLimited.ThrowIfNotSuccess(command);
                    }
                }

                // 3. 如果需要等待响应，创建等待任务
                if (waitForResponse && command.MessageType == MessageType.Business)
                {
                    var tcs = new UniTaskCompletionSource<ResponseMessage>();
                    pendingInfo = new PendingRequestInfo
                    {
                        Tcs = tcs,
                        CreatedTicks = Stopwatch.GetTimestamp(),
                        CmdInfo = command.CmdInfo,
                        MsgId = command.MsgId
                    };

                    if (!_pendingRequests.TryAdd(command.MsgId, pendingInfo))
                    {
                         PiscesClientCode.DuplicateMsgId.ThrowIfNotSuccess(command);
                    }
                    pendingAdded = true;
                }

                // 4. 发送消息
                var sendResult = SendInternal(command, out var packetLength);
                if (sendResult != PiscesClientCode.Success)
                {
                    _statistics.RecordSendFailed();
                    GameLogger.LogWarning($"[GameClient] 发送失败: {command.CmdInfo}, MsgId={command.MsgId}, 原因={sendResult}");
                    OnSendFailed?.Invoke(command.CmdInfo, command.MsgId, sendResult);
                    sendResult.ThrowIfNotSuccess(command);
                }

                // 5. 统计记录
                if (command.MessageType == MessageType.Heartbeat)
                    _statistics.RecordHeartbeatSend();

                _statistics.RecordSend(packetLength, command);

                // 6. 如果需要等待响应，等待
                if (waitForResponse && pendingInfo != null)
                {
                    using var timeoutCts = new CancellationTokenSource(_options.RequestTimeoutMs);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    try
                    {
                        response = await pendingInfo.Tcs.Task.AttachExternalCancellation(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        PiscesClientCode.Timeout.ThrowIfNotSuccess(command);
                    }
                }
                return response;
            }
            finally
            {
                if (command != null)
                {
                    // 7. 统一清理
                    if (pendingAdded)
                        _pendingRequests.TryRemove(command.MsgId, out _);

                    if (routeLocked)
                        UnlockRoute(command.CmdInfo);

                    // 回收请求指令
                    ReferencePool<RequestCommand>.Despawn(command);
                }

            }
        }

        #region 内部发送实现

        private PiscesClientCode SendInternal(RequestCommand command, out int packetLength)
        {
            var message = ReferencePool<ExternalMessage>.Spawn();
            message.MessageType = command.MessageType;
            message.CmdMerge = command.CmdInfo;
            message.MsgId = command.MsgId;
            message.Data = command.Data;

            var packet = PacketCodec.Encode(message);
            ReferencePool<ExternalMessage>.Despawn(message);

            if (!_channel.Send(packet))
            {
                packetLength = 0;
                return PiscesClientCode.ChannelError;
            }

            packetLength = packet.Length;
            return PiscesClientCode.Success;
        }

        #endregion

        #region 消息接收与处理逻辑

        private void OnChannelReceiveMessage(IProtocolChannel channel, ArraySegment<byte> data)
        {
            if (_disposed || data.Count == 0) return;

            try
            {
                _receiveBuffer.Write(data);
                var messages = _receiveBuffer.ReadPackets();
                foreach (var message in messages)
                {
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[GameClient] 消息包解析失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        private void ProcessMessage(ExternalMessage message)
        {
            if (message == null) return;

            switch (message.MessageType)
            {
                case MessageType.Heartbeat:
                    _heartbeatTimeoutCount = 0;
                    _statistics.RecordHeartbeatReceive();
                    break;
                case MessageType.TimeSync:
                    var timeSyncMsg = TimeSyncMessage.Parser.ParseFrom(message.Data);
                    TimeUtils.UpdateSync(timeSyncMsg.ClientTime, timeSyncMsg.ServerTime);
                    break;
                case MessageType.Disconnect:
                    ProcessDisconnectNotify(message);
                    break;
                default:
                    ProcessBusinessMessage(message);
                    break;
            }
        }

        private void ProcessBusinessMessage(ExternalMessage message)
        {
            _statistics.RecordReceive(message);

            var response = ReferencePool<ResponseMessage>.Spawn();
            response.Initialize(message);

            // 匹配等待中的 RequestAsync
            if (_pendingRequests.TryRemove(message.MsgId, out var pendingInfo))
            {
                // 触发 Task 完成（RequestAsync 里的 finally 会负责解锁路由）
                pendingInfo.Tcs?.TrySetResult(response);
            }

            // 触发外部通用的消息监听
            OnMessageReceived?.Invoke(response);

            // 回收资源
            ReferencePool<ResponseMessage>.Despawn(response);
        }

        private void ProcessDisconnectNotify(ExternalMessage message)
        {
            var disconnectNotify = DisconnectNotify.Parser.ParseFrom(message.Data);
            GameLogger.LogWarning(
                $"[GameClient] 收到服务器主动断线通知: Reason={disconnectNotify.Reason}, Msg={disconnectNotify.Message}");

            OnDisconnectNotify?.Invoke(disconnectNotify);

            // 如果该原因不允许自动重连（如：账号异地登陆、被封禁），则彻底关闭客户端
            if (!IsReconnectAllowed(disconnectNotify.Reason))
            {
                GameLogger.LogDebug($"[GameClient] 断线原因为强制退出，不再尝试重连。");
                Close();
            }
            else
            {
                // 正常断开，触发底层的断线逻辑（通常会进入重连流程）
                _channel?.Disconnect();
            }
        }

        #endregion
    }
}
