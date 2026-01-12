using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Channel;
using Pisces.Client.Network.Core;
using Pisces.Client.Utils;
using Pisces.Protocol;
using UnityEngine;

namespace Pisces.Client.Network
{
    /// <summary>
    /// 游戏客户端实现
    /// 提供连接管理、消息收发、心跳维护、自动重连等功能
    /// </summary>
    public partial class GameClient : IGameClient
    {
        private readonly GameClientOptions _options;
        private readonly NetworkStatistics _statistics;
        private readonly RateLimiter _rateLimiter;
        private readonly ConnectionStateMachine _stateMachine;
        private IProtocolChannel _channel;
        private PacketBuffer _receiveBuffer;

        private volatile bool _disposed;
        private volatile bool _isClosed;

        // 心跳相关
        private CancellationTokenSource _heartbeatCts;
        private int _heartbeatTimeoutCount;

        // 重连相关
        private CancellationTokenSource _reconnectCts;
        private readonly object _reconnectLock = new();
        private volatile bool _isReconnecting;
        private int _reconnectCount;

        /// <summary>
        /// 等待响应的请求队列
        /// Key: MsgId, Value: PendingRequestInfo（包含 TCS 和元数据）
        /// </summary>
        private readonly ConcurrentDictionary<int, PendingRequestInfo> _pendingRequests = new();

        public ConnectionState State => _stateMachine.CurrentState;

        public bool IsConnected =>
            _stateMachine.IsConnected && _channel?.IsConnected == true;

        public GameClientOptions Options => _options;

        /// <summary>
        /// 网络统计数据
        /// </summary>
        public NetworkStatistics Statistics => _statistics;

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
            _statistics = new NetworkStatistics();
            _receiveBuffer = new PacketBuffer(
                _options.PacketBufferInitialSize,
                _options.PacketBufferShrinkThreshold
            );
            _stateMachine = new ConnectionStateMachine();
            GameLogger.Enabled = _options.EnableLog;

            // 订阅状态机事件
            _stateMachine.OnStateChanged += HandleStateMachineStateChanged;

            // 初始化限流器
            if (_options.EnableRateLimit && _options.MaxSendRate > 0)
            {
                _rateLimiter = new RateLimiter(_options.MaxBurstSize, _options.MaxSendRate);
                GameLogger.Log($"[GameClient] 限流器已启用: 速率={_options.MaxSendRate}/s, 突发={_options.MaxBurstSize}");
            }
        }

        private void HandleStateMachineStateChanged(ConnectionState oldState, ConnectionState newState)
        {
            OnStateChanged?.Invoke(newState);
        }

        public void Connect()
        {
            ConnectAsync().Forget();
        }

        public async UniTask ConnectAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            if (_isClosed)
                throw new InvalidOperationException("客户端已关闭");

            // 使用状态机检查是否可以连接
            if (!_stateMachine.CanConnect)
            {
                if (_stateMachine.IsConnected)
                {
                    GameLogger.LogWarning("[GameClient] 已连接");
                    return;
                }
                if (_stateMachine.IsConnectingOrReconnecting)
                {
                    GameLogger.LogWarning("[GameClient] 正在连接中");
                    return;
                }
                throw new InvalidOperationException($"当前状态 {State} 不允许连接");
            }

            // 尝试转换到 Connecting 状态
            if (!_stateMachine.TryTransition(ConnectionState.Connecting, out _))
            {
                GameLogger.LogWarning("[GameClient] 状态转换失败，无法开始连接");
                return;
            }

            // 创建新通道（不立即赋值给 _channel，确保失败时能正确清理）
            var newChannel = ChannelFactory.Create(_options.ChannelType);

            try
            {
                // 初始化通道
                newChannel.OnInit();

                // 连接（带超时）
                using var cts = new CancellationTokenSource(_options.ConnectTimeoutMs);

                // 启动连接任务
                var connectTask = UniTask.RunOnThreadPool(
                    () => newChannel.Connect(_options.Host, _options.Port),
                    cancellationToken: cts.Token
                );

                // 等待连接完成或超时
                try
                {
                    await connectTask;

                    // 等待连接真正建立
                    await UniTask.WaitUntil(
                        () => newChannel.IsConnected,
                        cancellationToken: cts.Token
                    );
                }
                catch (OperationCanceledException)
                {
                    // 超时，清理新通道
                    CleanupChannel(newChannel);

                    _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
                    var ex = new TimeoutException(
                        $"Connect timeout after {_options.ConnectTimeoutMs}ms to {_options.Host}:{_options.Port}"
                    );
                    OnError?.Invoke(ex);
                    throw ex;
                }

                // 连接成功，清理旧通道并替换
                if (_channel != null)
                {
                    _channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                    _channel.DisconnectServerEvent -= OnChannelDisconnect;
                    CleanupChannel(_channel);
                }

                _channel = newChannel;

                // 订阅新通道事件
                _channel.ReceiveMessageEvent += OnChannelReceiveMessage;
                _channel.DisconnectServerEvent += OnChannelDisconnect;

                _stateMachine.TryTransition(ConnectionState.Connected, out _);
                _reconnectCount = 0;
                _statistics.RecordConnected();
                _statistics.ResetReconnectCount();

                // 启动心跳
                StartHeartbeat();

                // 启动待处理请求清理
                StartPendingRequestsCleanup();

                GameLogger.Log($"[GameClient] 已连接到 {_options.Host}:{_options.Port}");
            }
            catch (TimeoutException)
            {
                // 已在上面处理，直接抛出
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，清理新通道
                CleanupChannel(newChannel);

                _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
                GameLogger.LogError($"[GameClient] 连接失败: {ex.Message}");
                OnError?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// 清理通道资源
        /// </summary>
        private void CleanupChannel(IProtocolChannel channel)
        {
            if (channel == null)
                return;

            try
            {
                channel.Disconnect();
                if (channel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"[GameClient] 清理通道时发生异常: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            DisconnectAsync().Forget();
        }

        public async UniTask DisconnectAsync()
        {
            if (_disposed)
                return;

            StopHeartbeat();
            StopReconnect();
            StopPendingRequestsCleanup();

            if (_channel != null)
            {
                _channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                _channel.DisconnectServerEvent -= OnChannelDisconnect;
                _channel.Disconnect();
            }

            _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
            _statistics.RecordDisconnected();
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
            StopPendingRequestsCleanup();

            if (_channel != null)
            {
                _channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                _channel.DisconnectServerEvent -= OnChannelDisconnect;
                _channel.Disconnect();
            }

            _stateMachine.TryTransition(ConnectionState.Closed, out _);
            _statistics.RecordDisconnected();
            ClearPendingRequests(new OperationCanceledException("客户端已关闭"));

            GameLogger.Log("[GameClient] 已关闭");
        }

        private void OnChannelDisconnect(IProtocolChannel channel)
        {
            if (_disposed || _isClosed)
                return;

            // Unity 退出 Play Mode 时不处理
            if (!Application.isPlaying)
                return;

            GameLogger.LogWarning("[GameClient] 连接已断开");
            _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
            _statistics.RecordDisconnected();

            ClearPendingRequests(new OperationCanceledException("连接断开"));

            // 尝试自动重连
            if (_options.AutoReconnect && !_isClosed)
            {
                StartReconnect();
            }
        }

        private void ClearPendingRequests(Exception exception)
        {
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.Tcs?.TrySetException(exception);
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

            Debug.Log("释放？");
            if (disposing)
            {
                // 取消订阅状态机事件
                _stateMachine.OnStateChanged -= HandleStateMachineStateChanged;

                Close();
                
                _channel.Dispose();
                _channel = null;

                _receiveBuffer?.Clear();
                _receiveBuffer = null;

                _heartbeatCts?.Dispose();
                _reconnectCts?.Dispose();
                _cleanupCts?.Dispose();
            }

            GameLogger.Log("[GameClient] 已释放");
        }

        ~GameClient()
        {
            Dispose(false);
        }
    }
}
