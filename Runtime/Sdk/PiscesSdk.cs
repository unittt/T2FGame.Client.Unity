using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Pisces.Client.Network;
using Pisces.Client.Network.Core;
using Pisces.Client.Settings;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// Pisces Unity SDK 入口
    /// 提供高层次的 API 封装，简化游戏客户端的使用
    /// </summary>
    public class PiscesSdk : IDisposable
    {
        private static PiscesSdk _instance;
        private static readonly object _lock = new();

        // 三大核心管理器
        private ConnectionManager _connectionManager;
        private MessageRouter _messageRouter;
        private RequestManager _requestManager;

        private volatile bool _initialized;
        private volatile bool _disposed;

        /// <summary>
        /// 获取 SDK 单例实例
        /// </summary>
        public static PiscesSdk Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new PiscesSdk();
                }
            }
        }

        /// <summary>
        /// 获取游戏客户端实例
        /// </summary>
        public GameClient Client => _connectionManager?.Client;

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public ConnectionState State => _connectionManager?.State ?? ConnectionState.Disconnected;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _connectionManager?.IsConnected ?? false;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _initialized;

        #region 时间同步属性

        /// <summary>
        /// 是否已完成时间同步
        /// </summary>
        public bool IsTimeSynced => TimeUtils.IsSynced;

        /// <summary>
        /// 网络往返延迟（毫秒）
        /// </summary>
        public float RttMs => TimeUtils.RttMs;

        /// <summary>
        /// 服务器时间（毫秒时间戳）
        /// </summary>
        public long ServerTimeMs => TimeUtils.ServerTimeMs;

        /// <summary>
        /// 服务器时间（DateTime）
        /// </summary>
        public DateTime ServerTime => TimeUtils.ServerTime;

        #endregion

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event Action<ConnectionState> OnStateChanged;

        /// <summary>
        /// 收到服务器推送消息事件（原始消息）
        /// </summary>
        public event Action<ExternalMessage> OnMessageReceived;

        /// <summary>
        /// 连接错误事件
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// 收到服务器断线通知事件
        /// 在连接实际断开前触发，携带断线原因和消息
        /// 可用于显示断线提示 UI（如"账号在其他设备登录"）
        /// </summary>
        public event Action<DisconnectNotify> OnDisconnectNotify;

        private PiscesSdk() { }

        #region 初始化和连接

        /// <summary>
        /// 初始化 SDK（从 Project Settings 加载配置）
        /// </summary>
        public void Initialize()
        {
            var settings = PiscesSettings.Instance;
            GameClientOptions options = null;

            if (settings != null)
            {
                options = settings.ToGameClientOptions();
                GameLogger.Log($"[PiscesSdk] 从 Project Settings 加载配置: {settings.ActiveEnvironment.Name} ({settings.ActiveEnvironment.Host}:{settings.ActiveEnvironment.Port})");
            }
            else
            {
                GameLogger.LogWarning("[PiscesSdk] 未找到 PiscesSettings，使用默认配置");
            }

            Initialize(options);
        }

        /// <summary>
        /// 初始化 SDK
        /// </summary>
        /// <param name="options">客户端配置（传入 null 时使用默认配置）</param>
        public void Initialize(GameClientOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PiscesSdk));

            if (_initialized)
            {
                GameLogger.LogWarning("[PiscesSdk] SDK 已初始化，跳过");
                return;
            }

            // 初始化三大管理器
            _connectionManager = new ConnectionManager();
            _messageRouter = new MessageRouter();
            _requestManager = new RequestManager(_connectionManager);

            // 初始化连接管理器
            _connectionManager.Initialize(options);

            // 订阅事件
            _connectionManager.OnStateChanged += HandleStateChanged;
            _connectionManager.OnError += HandleError;
            _connectionManager.Client.OnMessageReceived += HandleMessageReceived;
            _connectionManager.Client.OnDisconnectNotify += HandleDisconnectNotify;
            _requestManager.OnError += HandleError;

            _initialized = true;
            GameLogger.Log("[PiscesSdk] SDK 初始化完成");
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async UniTask ConnectAsync()
        {
            EnsureInitialized();
            await _connectionManager.ConnectAsync();
        }

        /// <summary>
        /// 连接到指定服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public async UniTask ConnectAsync(string host, int port)
        {
            EnsureInitialized();
            await _connectionManager.ConnectAsync(host, port);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (!_initialized)
                return;
            await _connectionManager.DisconnectAsync();
        }

        /// <summary>
        /// 关闭连接（不再重连）
        /// </summary>
        public void Close()
        {
            _connectionManager?.Close();
        }

        /// <summary>
        /// 请求时间同步
        /// 发送时间同步请求到服务器，更新本地时间偏移
        /// </summary>
        public void RequestTimeSync()
        {
            if (!IsConnected)
                return;

            var command = RequestCommand.TimeSync();
            _connectionManager.Client.SendRequest(command);
        }

        #endregion

        #region 异步请求 (RequestAsync)

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(
            int cmdMerge,
            CancellationToken cancellationToken = default
        )
        {
            EnsureInitialized();
            return await _requestManager.RequestAsync(cmdMerge, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync<TRequest>(
            int cmdMerge,
            TRequest request,
            CancellationToken cancellationToken = default
        )
            where TRequest : IMessage
        {
            EnsureInitialized();
            return await _requestManager.RequestAsync(cmdMerge, request, cancellationToken);
        }

        /// <summary>
        /// 直接发送 RequestCommand 并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(
            RequestCommand command,
            CancellationToken cancellationToken = default
        )
        {
            EnsureInitialized();
            return await _requestManager.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        public async UniTask<TResponse> RequestAsync<TResponse>(
            int cmdMerge,
            CancellationToken cancellationToken = default
        )
            where TResponse : IMessage, new()
        {
            EnsureInitialized();
            return await _requestManager.RequestAsync<TResponse>(cmdMerge, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        public async UniTask<TResponse> RequestAsync<TRequest, TResponse>(
            int cmdMerge,
            TRequest request,
            CancellationToken cancellationToken = default
        )
            where TRequest : IMessage
            where TResponse : IMessage, new()
        {
            EnsureInitialized();
            return await _requestManager.RequestAsync<TRequest, TResponse>(
                cmdMerge,
                request,
                cancellationToken
            );
        }

        #endregion

        #region 发送不等待响应 (Send)

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send(int cmdMerge)
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge);
        }

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send<TRequest>(int cmdMerge, TRequest request)
            where TRequest : IMessage
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge, request);
        }

        /// <summary>
        /// 直接发送 RequestCommand（仅发送，不等待响应）
        /// </summary>
        public void Send(RequestCommand command)
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(command);
        }
        #endregion

        #region 带回调的发送 (Send with Callback)

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，原始响应）
        /// </summary>
        public void Send(int cmdMerge, Action<ResponseMessage> callback)
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge, callback);
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，泛型响应）
        /// </summary>
        public void Send<TResponse>(int cmdMerge, Action<TResponse> callback)
            where TResponse : IMessage, new()
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge, callback);
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，泛型响应）
        /// </summary>
        public void Send<TRequest, TResponse>(
            int cmdMerge,
            TRequest request,
            Action<TResponse> callback
        )
            where TRequest : IMessage
            where TResponse : IMessage, new()
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge, request, callback);
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，原始响应）
        /// </summary>
        public void Send<TRequest>(int cmdMerge, TRequest request, Action<ResponseMessage> callback)
            where TRequest : IMessage
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(cmdMerge, request, callback);
        }

        /// <summary>
        /// 直接发送 RequestCommand 并在收到响应时执行回调
        /// </summary>
        public void Send(RequestCommand command, Action<ResponseMessage> callback)
        {
            if (!IsInitialized)
                return;
            _requestManager.Send(command, callback);
        }

        #endregion

        #region 消息订阅 (Subscribe)

        /// <summary>
        /// 订阅指定 cmdMerge 的服务器推送消息
        /// </summary>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe(int cmdMerge, Action<ExternalMessage> callback)
        {
            EnsureInitialized();
            return _messageRouter.Subscribe(cmdMerge, callback);
        }

        /// <summary>
        /// 订阅指定 cmdMerge 的服务器推送消息（泛型版本，自动解包）
        /// </summary>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe<TMessage>(int cmdMerge, Action<TMessage> callback)
            where TMessage : IMessage, new()
        {
            EnsureInitialized();
            return _messageRouter.Subscribe(cmdMerge, callback);
        }

        /// <summary>
        /// 订阅指定 cmdMerge 的服务器推送消息（使用 MessageParser，性能更优）
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="callback">消息处理回调</param>
        /// <param name="parser">消息解析器，通常使用 YourMessage.Parser</param>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe<TMessage>(int cmdMerge, Action<TMessage> callback, MessageParser<TMessage> parser)
            where TMessage : IMessage<TMessage>
        {
            EnsureInitialized();
            return _messageRouter.Subscribe(cmdMerge, callback, parser);
        }

        /// <summary>
        /// 清除该 cmdMerge 的所有订阅
        /// </summary>
        /// <param name="cmdMerge"></param>
        public void UnsubscribeAll(int cmdMerge)
        {
            if (!IsInitialized)
                return;

            _messageRouter.Clear(cmdMerge);
        }

        /// <summary>
        /// 取消所有消息订阅
        /// </summary>
        public void UnsubscribeAll()
        {
            if (!IsInitialized)
                return;
            _messageRouter.ClearAll();
        }

        #endregion

        #region 事件处理

        private void HandleStateChanged(ConnectionState state)
        {
            OnStateChanged?.Invoke(state);
        }

        private void HandleMessageReceived(ExternalMessage message)
        {
            // 先触发通用的消息接收事件
            OnMessageReceived?.Invoke(message);

            // 然后通过路由器分发给订阅者
            try
            {
                _messageRouter.Dispatch(message);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[PiscesSdk] 消息分发错误: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        private void HandleError(Exception ex)
        {
            OnError?.Invoke(ex);
        }

        private void HandleDisconnectNotify(DisconnectNotify notify)
        {
            OnDisconnectNotify?.Invoke(notify);
        }

        #endregion

        #region 工具方法

        private void EnsureInitialized()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PiscesSdk));

            if (!_initialized)
                throw new InvalidOperationException("SDK 未初始化，请先调用 Initialize()");
        }

        #endregion

        #region Dispose

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
                // 取消订阅事件
                if (_connectionManager != null)
                {
                    _connectionManager.OnStateChanged -= HandleStateChanged;
                    _connectionManager.OnError -= HandleError;

                    if (_connectionManager.Client != null)
                    {
                        _connectionManager.Client.OnMessageReceived -= HandleMessageReceived;
                        _connectionManager.Client.OnDisconnectNotify -= HandleDisconnectNotify;
                    }
                }

                if (_requestManager != null)
                {
                    _requestManager.OnError -= HandleError;
                }

                // 释放管理器
                _connectionManager?.Dispose();
                _messageRouter?.ClearAll();

                // 重置时间同步状态
                TimeUtils.Reset();

                _connectionManager = null;
                _messageRouter = null;
                _requestManager = null;

                _initialized = false;
            }

            GameLogger.Log("[PiscesSdk] SDK 已释放");
        }
        
        /// <summary>
        /// 重置 SDK 单例（主要用于测试）
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        ~PiscesSdk()
        {
            Dispose(false);
        }

        #endregion
    }
}
