using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Channel;
using Pisces.Client.Network.Core;
using Pisces.Client.Sdk;
using Pisces.Client.Unity;
using Pisces.Client.Utils;
using Pisces.Protocol;

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
        private readonly ConnectionStateMachine _stateMachine;
        private IProtocolChannel _channel;

        private volatile bool _disposed;
        private volatile bool _isClosed;
        private volatile bool _isManualDisconnect;

        public ConnectionState State => _stateMachine.CurrentState;

        public bool IsConnected => _stateMachine.IsConnected && _channel?.IsConnected == true;

        public GameClientOptions Options => _options;

        /// <summary>
        /// 网络统计数据
        /// </summary>
        internal NetworkStatistics Statistics => _statistics;

        #region 事件
        public event Action<ConnectionState> OnStateChanged;
        public event Action<Exception> OnError;

        /// <summary>
        /// 收到服务器断线通知事件
        /// 在连接实际断开前触发，携带断线原因和消息
        /// </summary>
        public event Action<DisconnectNotify> OnDisconnectNotify;
        #endregion


        public GameClient(GameClientOptions options = null)
        {
            _options = options?.Clone() ?? new GameClientOptions();
            _statistics = new NetworkStatistics();
            _stateMachine = new ConnectionStateMachine();
            GameLogger.Level = _options.LogLevel;

            // 初始化消息模块
            InitMessaging();

            // 订阅状态机事件
            _stateMachine.OnStateChanged += HandleStateMachineStateChanged;
        }

        private void HandleStateMachineStateChanged(ConnectionState oldState, ConnectionState newState)
        {
            try
            {
                OnStateChanged?.Invoke(newState);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[GameClient] 状态变更事件处理异常: {ex.Message}");
            }
        }

        public void Connect()
        {
            ConnectAsync().Forget();
        }

        public async UniTask ConnectAsync()
        {
            if (_disposed || _isClosed)
                PiscesClientCode.ClientClosed.ThrowIfFailed();

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
                PiscesClientCode.ConnectionFailed.ThrowIfFailed($"State {State} does not allow connection.");
            }

            // 尝试转换到 Connecting 状态
            if (!_stateMachine.TryTransition(ConnectionState.Connecting, out _))
            {
                GameLogger.LogWarning("[GameClient] 状态转换失败，无法开始连接");
                return;
            }

            // 重置手动断开标志
            _isManualDisconnect = false;

            // 创建新通道（不立即赋值给 _channel，确保失败时能正确清理）
            IProtocolChannel newChannel = null;

            try
            {
                newChannel = ChannelFactory.Create(_options.ChannelType);

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
                await connectTask;

                // 等待连接真正建立
                await UniTask.WaitUntil(() => newChannel.IsConnected, cancellationToken: cts.Token);

                // 连接成功，完成后续处理
                FinalizeConnection(newChannel, isReconnect: false);
            }
            catch (OperationCanceledException ex)// 4. 精确映射异常
            {
                CleanupChannel(newChannel);

                OnError?.Invoke(ex);
                PiscesClientCode.Timeout.ThrowIfFailed($"Connect timeout to {_options.Host}:{_options.Port}", ex);
            }
            catch (Exception ex)
            {
                CleanupChannel(newChannel);

                _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
                GameLogger.LogError($"[GameClient] 连接失败: {ex.Message}");
                OnError?.Invoke(ex);
                PiscesClientCode.ConnectionFailed.ThrowIfFailed(null,ex);
            }
        }

        /// <summary>
        /// 完成连接建立后的处理
        /// </summary>
        /// <param name="newChannel">新建立的通道</param>
        /// <param name="isReconnect">是否为重连</param>
        private void FinalizeConnection(IProtocolChannel newChannel, bool isReconnect)
        {
            // 1. 清理旧通道并替换
            var oldChannel = Interlocked.Exchange(ref _channel, newChannel);
            CleanupChannel(oldChannel);

            // 2. 订阅新通道事件
            _channel.ReceiveMessageEvent += OnChannelReceiveMessage;
            _channel.DisconnectServerEvent += OnChannelDisconnect;

            // 3. 状态转换和统计
            _stateMachine.TryTransition(ConnectionState.Connected, out _);
            _statistics.RecordConnected();
            _statistics.ResetReconnectCount();

            // 4. 启动服务
            StartHeartbeat();
            StartPendingRequests();

            // 5. 日志
            var connectType = isReconnect ? "重连" : "连接";
            GameLogger.Log($"[GameClient] {connectType}成功 - {_options.Host}:{_options.Port}");
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
                // 先取消事件订阅，避免事件处理器在清理过程中被触发
                channel.ReceiveMessageEvent -= OnChannelReceiveMessage;
                channel.DisconnectServerEvent -= OnChannelDisconnect;
                // 断开连接
                channel.Disconnect();
                // 释放资源
                channel.Dispose();
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"[GameClient] 清理通道时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止所有后台服务
        /// </summary>
        private void StopAllServices()
        {
            StopHeartbeat();
            StopReconnect();
            StopPendingRequests();
        }

        /// <summary>
        /// 清理连接资源
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <param name="reason">断开原因</param>
        private void CleanupConnection(ConnectionState targetState, string reason)
        {
            _isManualDisconnect = true;

            StopAllServices();

            var channel = _channel;
            CleanupChannel(channel);

            _stateMachine.TryTransition(targetState, out _);
            _statistics.RecordDisconnected();
            ClearPendingRequests(PiscesClientCode.ClientClosed);
            ClearLockedRoutes();
        }

        public void Disconnect()
        {
            DisconnectAsync().Forget();
        }

        public async UniTask DisconnectAsync()
        {
            if (_disposed)
                return;

            CleanupConnection(ConnectionState.Disconnected, "Disconnected");
            await UniTask.CompletedTask;
        }

        public void Close()
        {
            if (_disposed)
                return;

            _isClosed = true;
            CleanupConnection(ConnectionState.Closed, "客户端已关闭");
            GameLogger.LogDebug("[GameClient] 已关闭");
        }

        private void OnChannelDisconnect(IProtocolChannel channel)
        {
            if (_disposed || _isClosed || _isManualDisconnect)
                return;

            // Unity 退出 Play Mode 时不处理
            if (!PiscesLifecycleManager.IsPlaying)
                return;

            GameLogger.LogWarning("[GameClient] 连接已断开");
            _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
            _statistics.RecordDisconnected();

            ClearPendingRequests(PiscesClientCode.NotConnected);
            ClearLockedRoutes();

            // 尝试自动重连
            if (_options.AutoReconnect && !_isClosed)
            {
                StartReconnect();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                try
                {
                    // 取消订阅状态机事件
                    _stateMachine.OnStateChanged -= HandleStateMachineStateChanged;

                    // Close 内部会调用 StopAllServices
                    Close();

                    // 释放通道引用
                    var channel = Interlocked.Exchange(ref _channel, null);
                    channel?.Dispose();

                    // 释放各部分类的额外资源
                    DisposeMessaging();
                    DisposeHeartbeat();
                    DisposeReconnect();
                    DisposePendingRequests();
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[GameClient] 释放资源时异常: {ex.Message}");
                }
            }

            GameLogger.LogDebug("[GameClient] 已释放");
        }

        ~GameClient()
        {
            Dispose(false);
        }
    }
}
