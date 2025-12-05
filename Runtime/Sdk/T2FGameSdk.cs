using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using T2FGame.Client.Network;
using T2FGame.Client.Protocol;
using T2FGame.Client.Utils;
using T2FGame.Protocol;

namespace T2FGame.Client.Sdk
{
    /// <summary>
    /// T2F 游戏 SDK 入口
    /// 提供高层次的 API 封装，简化游戏客户端的使用
    /// </summary>
    public class T2FGameSdk : IDisposable
    {
        private static T2FGameSdk _instance;
        private static readonly object _lock = new object();

        private GameClient _client;
        private GameClientOptions _options;
        private volatile bool _initialized;
        private volatile bool _disposed;

        /// <summary>
        /// 获取 SDK 单例实例
        /// </summary>
        public static T2FGameSdk Instance
        {
            get { return _instance ??= new T2FGameSdk(); }
        }

        /// <summary>
        /// 获取游戏客户端实例
        /// </summary>
        public GameClient Client => _client;

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public ConnectionState State => _client?.State ?? ConnectionState.Disconnected;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _client?.IsConnected ?? false;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event Action<ConnectionState> OnStateChanged;

        /// <summary>
        /// 收到服务器推送消息事件
        /// </summary>
        public event Action<ExternalMessage> OnMessageReceived;

        /// <summary>
        /// 连接错误事件
        /// </summary>
        public event Action<Exception> OnError;

        private T2FGameSdk()
        {
        }

        /// <summary>
        /// 初始化 SDK
        /// </summary>
        /// <param name="options">客户端配置</param>
        public void Initialize(GameClientOptions options = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(T2FGameSdk));

            if (_initialized)
            {
                GameLogger.LogWarning("[T2FGameSdk] SDK 已初始化，跳过");
                return;
            }

            _options = options?.Clone() ?? new GameClientOptions();
            _client = new GameClient(_options);

            // 订阅客户端事件
            _client.OnStateChanged += HandleStateChanged;
            _client.OnMessageReceived += HandleMessageReceived;
            _client.OnError += HandleError;

            _initialized = true;
            GameLogger.Log("[T2FGameSdk] SDK 初始化完成");
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async UniTask ConnectAsync()
        {
            EnsureInitialized();
            await _client.ConnectAsync();
        }

        /// <summary>
        /// 连接到指定服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public async UniTask ConnectAsync(string host, int port)
        {
            EnsureInitialized();

            _options.Host = host;
            _options.Port = port;

            // 重新创建客户端以应用新配置
            if (_client != null)
            {
                _client.OnStateChanged -= HandleStateChanged;
                _client.OnMessageReceived -= HandleMessageReceived;
                _client.OnError -= HandleError;
                _client.Dispose();
            }

            _client = new GameClient(_options);
            _client.OnStateChanged += HandleStateChanged;
            _client.OnMessageReceived += HandleMessageReceived;
            _client.OnError += HandleError;

            await _client.ConnectAsync();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (_client == null) return;
            await _client.DisconnectAsync();
        }

        /// <summary>
        /// 关闭连接（不再重连）
        /// </summary>
        public void Close()
        {
            _client?.Close();
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息</returns>
        public async UniTask<ResponseMessage> RequestAsync(int cmdMerge, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            var command = RequestCommand.Of(cmdMerge);
            return await _client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="request">请求数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息</returns>
        public async UniTask<ResponseMessage> RequestAsync<TRequest>(int cmdMerge, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : Google.Protobuf.IMessage
        {
            EnsureConnected();
            var command = RequestCommand.Of(cmdMerge, request);
            return await _client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应数据</returns>
        public async UniTask<TResponse> RequestAsync<TResponse>(int cmdMerge, CancellationToken cancellationToken = default)
            where TResponse : Google.Protobuf.IMessage, new()
        {
            var response = await RequestAsync(cmdMerge, cancellationToken);

            if (response.HasError)
            {
                throw new Exception($"Request failed with status: {response.ResponseStatus}");
            }

            return response.GetValue<TResponse>();
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="request">请求数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应数据</returns>
        public async UniTask<TResponse> RequestAsync<TRequest, TResponse>(int cmdMerge, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : Google.Protobuf.IMessage
            where TResponse : Google.Protobuf.IMessage, new()
        {
            var response = await RequestAsync(cmdMerge, request, cancellationToken);

            if (response.HasError)
            {
                throw new Exception($"Request failed with status: {response.ResponseStatus}");
            }

            return response.GetValue<TResponse>();
        }

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        public void Send(int cmdMerge)
        {
            if (!IsConnected) return;
            var command = RequestCommand.Of(cmdMerge);
            _client.SendRequest(command);
        }

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="request">请求数据</param>
        public void Send<TRequest>(int cmdMerge, TRequest request)
            where TRequest : Google.Protobuf.IMessage
        {
            if (!IsConnected) return;
            var command = RequestCommand.Of(cmdMerge, request);
            _client.SendRequest(command);
        }

        /// <summary>
        /// 发送整数请求
        /// </summary>
        public void SendInt(int cmdMerge, int value)
        {
            if (!IsConnected) return;
            var command = RequestCommand.OfInt(cmdMerge, value);
            _client.SendRequest(command);
        }

        /// <summary>
        /// 发送字符串请求
        /// </summary>
        public void SendString(int cmdMerge, string value)
        {
            if (!IsConnected) return;
            var command = RequestCommand.OfString(cmdMerge, value);
            _client.SendRequest(command);
        }

        /// <summary>
        /// 发送长整数请求
        /// </summary>
        public void SendLong(int cmdMerge, long value)
        {
            if (!IsConnected) return;
            var command = RequestCommand.OfLong(cmdMerge, value);
            _client.SendRequest(command);
        }

        /// <summary>
        /// 发送布尔值请求
        /// </summary>
        public void SendBool(int cmdMerge, bool value)
        {
            if (!IsConnected) return;
            var command = RequestCommand.OfBool(cmdMerge, value);
            _client.SendRequest(command);
        }

        private void HandleStateChanged(ConnectionState state)
        {
            OnStateChanged?.Invoke(state);
        }

        private void HandleMessageReceived(ExternalMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }

        private void HandleError(Exception ex)
        {
            OnError?.Invoke(ex);
        }

        private void EnsureInitialized()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(T2FGameSdk));

            if (!_initialized)
                throw new InvalidOperationException("SDK 未初始化，请先调用 Initialize()");
        }

        private void EnsureConnected()
        {
            EnsureInitialized();

            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            if (disposing)
            {
                if (_client != null)
                {
                    _client.OnStateChanged -= HandleStateChanged;
                    _client.OnMessageReceived -= HandleMessageReceived;
                    _client.OnError -= HandleError;
                    _client.Dispose();
                    _client = null;
                }

                _initialized = false;
            }

            GameLogger.Log("[T2FGameSdk] SDK 已释放");
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

        ~T2FGameSdk()
        {
            Dispose(false);
        }
    }
}
