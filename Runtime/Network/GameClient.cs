using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using T2FGame.Client.Network.Channel;
using T2FGame.Client.Sdk;
using T2FGame.Client.Utils;
using T2FGame.Protocol;

namespace T2FGame.Client.Network
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
                GameLogger.Log($"[GameClient] State changed: {oldState} -> {value}");
                OnStateChanged?.Invoke(value);
            }
        }

        public bool IsConnected =>
            _state == ConnectionState.Connected && _channel?.IsConnected == true;

        public GameClientOptions Options => _options;

        public event Action<ConnectionState> OnStateChanged;
        public event Action<ExternalMessage> OnMessageReceived;
        public event Action<Exception> OnError;

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
                throw new InvalidOperationException("Client has been closed");

            if (State is ConnectionState.Connected or ConnectionState.Connecting)
            {
                GameLogger.LogWarning("[GameClient] Already connected or connecting");
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

                GameLogger.Log($"[GameClient] Connected to {_options.Host}:{_options.Port}");
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
                GameLogger.LogError($"[GameClient] Connect failed: {ex.Message}");
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
            ClearPendingRequests(new OperationCanceledException("Client closed"));

            GameLogger.Log("[GameClient] Closed");
        }

        public async UniTask SendAsync(
            ExternalMessage message,
            CancellationToken cancellationToken = default
        )
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

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
                GameLogger.LogError($"[GameClient] Send failed: {ex.Message}");
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
                throw new InvalidOperationException("Not connected");

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var tcs = new UniTaskCompletionSource<ResponseMessage>();

            // 注册等待响应
            if (command.CommandType == CommandType.Business && !_pendingRequests.TryAdd(command.MsgId, tcs))
            {
                throw new InvalidOperationException($"Duplicate MsgId: {command.MsgId}");
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
                CmdCode = (int)command.CommandType,
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
                GameLogger.LogError($"[GameClient] Process message failed: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        private void ProcessMessage(ExternalMessage message)
        {
            if (message == null)
                return;

            // 判断是心跳响应还是业务消息
            if (message.CmdCode == (int)CommandType.Heartbeat)
            {
                // 心跳响应，重置超时计数
                _heartbeatTimeoutCount = 0;
                GameLogger.Log("[GameClient] Heartbeat response received");
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
        }

        private void OnChannelDisconnect(IProtocolChannel channel)
        {
            if (_disposed || _isClosed)
                return;

            GameLogger.LogWarning("[GameClient] Connection lost");
            State = ConnectionState.Disconnected;

            ClearPendingRequests(new OperationCanceledException("Connection lost"));

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
                            $"[GameClient] Heartbeat timeout ({_heartbeatTimeoutCount} times)"
                        );
                        _channel?.Disconnect();
                        break;
                    }

                    // 发送心跳
                    var heartbeat = RequestCommand.Heartbeat();
                    SendRequest(heartbeat);
                    _heartbeatTimeoutCount++;

                    GameLogger.Log("[GameClient] Heartbeat sent");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[GameClient] Heartbeat error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Reconnect

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
                    GameLogger.LogWarning(
                        $"[GameClient] Max reconnect attempts reached ({_reconnectCount})"
                    );
                    State = ConnectionState.Disconnected;
                    break;
                }

                _reconnectCount++;
                GameLogger.Log($"[GameClient] Reconnecting... (attempt {_reconnectCount})");

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
                        GameLogger.Log("[GameClient] Reconnected successfully");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogWarning($"[GameClient] Reconnect failed: {ex.Message}");
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

            GameLogger.Log("[GameClient] Disposed");
        }

        ~GameClient()
        {
            Dispose(false);
        }
    }
}
