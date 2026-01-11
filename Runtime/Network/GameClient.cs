using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Channel;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Network
{
    /// <summary>
    /// 游戏客户端实现
    /// 提供连接管理、消息收发、心跳维护、自动重连等功能
    /// </summary>
    public class GameClient : IGameClient
    {
        private readonly GameClientOptions _options;
        private IProtocolChannel _channel;
        private PacketBuffer _receiveBuffer;

        private volatile bool _disposed;
        private volatile bool _isClosed;
        private volatile ConnectionState _state = ConnectionState.Disconnected;

        private CancellationTokenSource _heartbeatCts;
        private CancellationTokenSource _reconnectCts;
        private int _heartbeatTimeoutCount;
        private int _reconnectCount;

        /// <summary>
        /// 等待响应的请求队列
        /// Key: MsgId, Value: TaskCompletionSource
        /// </summary>
        private readonly ConcurrentDictionary<
            int,
            UniTaskCompletionSource<ResponseMessage>
        > _pendingRequests = new();

        public ConnectionState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                    return;
                var oldState = _state;
                _state = value;
                GameLogger.Log($"[GameClient] 连接状态变化: {oldState} -> {value}");
                OnStateChanged?.Invoke(value);
            }
        }

        public bool IsConnected =>
            _state == ConnectionState.Connected && _channel?.IsConnected == true;

        public GameClientOptions Options => _options;

        public event Action<ConnectionState> OnStateChanged;
        public event Action<ExternalMessage> OnMessageReceived;
        public event Action<Exception> OnError;

        /// <summary>
        /// 收到服务器断线通知事件
        /// 在连接实际断开前触发，携带断线原因和消息
        /// </summary>
        public event Action<DisconnectNotify> OnDisconnectNotify;

        public GameClient(GameClientOptions options = null)
        {
            _options = options?.Clone() ?? new GameClientOptions();
            _receiveBuffer = new PacketBuffer(_options.ReceiveBufferSize);
            GameLogger.Enabled = _options.EnableLog;
        }

        public async UniTask ConnectAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (_isClosed)
                throw new InvalidOperationException("客户端已关闭");

            if (State is ConnectionState.Connected or ConnectionState.Connecting)
            {
                GameLogger.LogWarning("[GameClient] 已连接或正在连接中");
                return;
            }

            State = ConnectionState.Connecting;

            try
            {
                // 创建通道
                _channel?.Disconnect();
                _channel = ChannelFactory.Create(_options.ChannelType);

                // 订阅通道事件
                _channel.ReceiveMessageEvent += OnChannelReceiveMessage;
                _channel.DisconnectServerEvent += OnChannelDisconnect;

                // 初始化通道
                _channel.OnInit();

                // 连接（带超时）
                using var cts = new CancellationTokenSource(_options.ConnectTimeoutMs);

                await UniTask.RunOnThreadPool(
                    () =>
                    {
                        _channel.Connect(_options.Host, _options.Port);
                    },
                    cancellationToken: cts.Token
                );

                // 等待连接建立
                await UniTask.WaitUntil(() => _channel.IsConnected, cancellationToken: cts.Token);

                State = ConnectionState.Connected;
                _reconnectCount = 0;

                // 启动心跳
                StartHeartbeat();

                GameLogger.Log($"[GameClient] 已连接到 {_options.Host}:{_options.Port}");
            }
            catch (OperationCanceledException)
            {
                State = ConnectionState.Disconnected;
                var ex = new TimeoutException(
                    $"Connect timeout after {_options.ConnectTimeoutMs}ms"
                );
                OnError?.Invoke(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                GameLogger.LogError($"[GameClient] 连接失败: {ex.Message}");
                OnError?.Invoke(ex);
                throw;
            }
        }

        public async UniTask DisconnectAsync()
        {
            if (_disposed)
                return;

            StopHeartbeat();
            StopReconnect();

            if (_channel != null)
            {
                _channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                _channel.DisconnectServerEvent -= OnChannelDisconnect;
                _channel.Disconnect();
            }

            State = ConnectionState.Disconnected;
            ClearPendingRequests(new OperationCanceledException("Disconnected"));

            await UniTask.CompletedTask;
        }

        public void Close()
        {
            if (_disposed)
                return;

            _isClosed = true;
            StopHeartbeat();
            StopReconnect();

            if (_channel != null)
            {
                _channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                _channel.DisconnectServerEvent -= OnChannelDisconnect;
                _channel.Disconnect();
            }

            State = ConnectionState.Closed;
            ClearPendingRequests(new OperationCanceledException("客户端已关闭"));

            GameLogger.Log("[GameClient] 已关闭");
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
                _channel.Send(packet);

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
        public async UniTask<ResponseMessage> RequestAsync(
            RequestCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (!IsConnected)
                throw new InvalidOperationException("未连接");

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var tcs = new UniTaskCompletionSource<ResponseMessage>();

            // 注册等待响应
            if (
                command.MessageType == MessageType.Business
                && !_pendingRequests.TryAdd(command.MsgId, tcs)
            )
            {
                throw new InvalidOperationException($"重复的 MsgId: {command.MsgId}");
            }

            try
            {
                // 发送请求
                var packet = PacketCodec.Encode(CreateExternalMessage(command));
                _channel.Send(packet);

                // 等待响应（带超时）
                using var timeoutCts = new CancellationTokenSource(_options.RequestTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCts.Token
                );

                var response = await tcs.Task.AttachExternalCancellation(linkedCts.Token);
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
        public void SendRequest(RequestCommand command)
        {
            if (_disposed || !IsConnected || command == null)
                return;

            try
            {
                var packet = PacketCodec.Encode(CreateExternalMessage(command));
                _channel.Send(packet);
            }
            finally
            {
                ReferencePool<RequestCommand>.Despawn(command);
            }
        }

        private ExternalMessage CreateExternalMessage(RequestCommand command)
        {
            return new ExternalMessage
            {
                MessageType = command.MessageType,
                CmdMerge = command.CmdMerge,
                MsgId = command.MsgId,
                Data = command.Data,
            };
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
            if (message == null)
                return;

            // 判断是心跳响应还是业务消息
            if (message.MessageType == MessageType.Heartbeat)
            {
                // 心跳响应，重置超时计数
                _heartbeatTimeoutCount = 0;
                GameLogger.Log("[GameClient] 收到心跳响应");
                return;
            }

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
                    GameLogger.Log($"[GameClient] 断线原因 {disconnectNotify.Reason} 不允许重连，关闭客户端");
                    Close();
                }
                else
                {
                    // 允许重连的情况，正常断开（会触发自动重连）
                    _channel?.Disconnect();
                }

                return;
            }

            // 创建响应消息
            var response = ReferencePool<ResponseMessage>.Spawn();
            response.Initialize(message);

            // 尝试匹配等待的请求
            if (_pendingRequests.TryRemove(message.MsgId, out var tcs))
            {
                // 匹配到请求，设置响应结果
                tcs.TrySetResult(response);
            }

            // 触发消息接收事件（所有业务消息都触发，包括请求响应和服务器推送）
            OnMessageReceived?.Invoke(message);
            
            // 归还响应消息
            ReferencePool<ResponseMessage>.Despawn(response);
        }

        private void OnChannelDisconnect(IProtocolChannel channel)
        {
            if (_disposed || _isClosed)
                return;

            GameLogger.LogWarning("[GameClient] 连接已断开");
            State = ConnectionState.Disconnected;

            ClearPendingRequests(new OperationCanceledException("连接断开"));

            // 尝试自动重连
            if (_options.AutoReconnect && !_isClosed)
            {
                StartReconnect();
            }
        }

        #region Heartbeat

        private void StartHeartbeat()
        {
            StopHeartbeat();

            _heartbeatCts = new CancellationTokenSource();
            _heartbeatTimeoutCount = 0;

            HeartbeatLoop(_heartbeatCts.Token).Forget();
        }

        private void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        private async UniTaskVoid HeartbeatLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_options.HeartbeatIntervalSec),
                        cancellationToken: cancellationToken
                    );

                    if (!IsConnected)
                        break;

                    // 检查是否超时
                    if (_heartbeatTimeoutCount >= _options.HeartbeatTimeoutCount)
                    {
                        GameLogger.LogWarning(
                            $"[GameClient] 心跳超时 ({_heartbeatTimeoutCount} 次)"
                        );
                        _channel?.Disconnect();
                        break;
                    }

                    // 发送心跳
                    var heartbeat = RequestCommand.Heartbeat();
                    SendRequest(heartbeat);
                    _heartbeatTimeoutCount++;

                    GameLogger.Log("[GameClient] 已发送心跳");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[GameClient] 心跳错误: {ex.Message}");
                }
            }
        }

        #endregion

        #region Reconnect

        /// <summary>
        /// 判断断线原因是否允许自动重连
        /// </summary>
        /// <param name="reason">断线原因</param>
        /// <returns>是否允许重连</returns>
        private static bool IsReconnectAllowed(DisconnectReason reason)
        {
            return reason switch
            {
                // 不允许重连的情况
                DisconnectReason.DuplicateLogin => false,        // 重复登录（被顶号）
                DisconnectReason.Banned => false,                // 被封禁
                DisconnectReason.ServerMaintenance => false,     // 服务器维护
                DisconnectReason.AuthenticationFailed => false,  // 认证失败
                DisconnectReason.ServerClose => false,           // 服务器关闭

                // 允许重连的情况
                DisconnectReason.Unknown => true,                // 未知原因
                DisconnectReason.ClientClose => true,            // 客户端关闭（一般不会从服务器发来）
                DisconnectReason.IdleTimeout => true,            // 空闲超时
                DisconnectReason.NetworkError => true,           // 网络错误

                // 默认允许重连
                _ => true
            };
        }

        private void StartReconnect()
        {
            if (_isClosed || _disposed)
                return;

            StopReconnect();

            _reconnectCts = new CancellationTokenSource();
            State = ConnectionState.Reconnecting;

            ReconnectLoop(_reconnectCts.Token).Forget();
        }

        private void StopReconnect()
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;
        }

        private async UniTaskVoid ReconnectLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isClosed)
            {
                // 检查重连次数
                if (_options.MaxReconnectCount > 0 && _reconnectCount >= _options.MaxReconnectCount)
                {
                    GameLogger.LogWarning($"[GameClient] 达到最大重连次数 ({_reconnectCount})");
                    State = ConnectionState.Disconnected;
                    break;
                }

                _reconnectCount++;
                GameLogger.Log($"[GameClient] 正在重连... (第 {_reconnectCount} 次尝试)");

                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_options.ReconnectIntervalSec),
                        cancellationToken: cancellationToken
                    );

                    // 重置状态
                    State = ConnectionState.Connecting;

                    // 尝试连接
                    await ConnectAsync();

                    if (IsConnected)
                    {
                        GameLogger.Log("[GameClient] 重连成功");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogWarning($"[GameClient] 重连失败: {ex.Message}");
                    State = ConnectionState.Reconnecting;
                }
            }
        }

        #endregion

        private void ClearPendingRequests(Exception exception)
        {
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetException(exception);
            }
            _pendingRequests.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                Close();

                if (_channel is IDisposable disposableChannel)
                {
                    disposableChannel.Dispose();
                }
                _channel = null;

                _receiveBuffer?.Clear();
                _receiveBuffer = null;

                _heartbeatCts?.Dispose();
                _reconnectCts?.Dispose();
            }

            GameLogger.Log("[GameClient] 已释放");
        }

        ~GameClient()
        {
            Dispose(false);
        }
    }
}
