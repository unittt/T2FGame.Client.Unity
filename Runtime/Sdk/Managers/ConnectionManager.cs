using System;
using Cysharp.Threading.Tasks;
using T2FGame.Client.Network;
using T2FGame.Client.Utils;

namespace T2FGame.Client.Sdk
{
    /// <summary>
    /// 连接管理器
    /// 负责管理客户端连接、断开、状态监控等
    /// </summary>
    internal sealed class ConnectionManager : IDisposable
    {
        private GameClient _client;
        private GameClientOptions _options;
        private volatile bool _disposed;

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
        /// 连接状态变化事件
        /// </summary>
        public event Action<ConnectionState> OnStateChanged;

        /// <summary>
        /// 连接错误事件
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// 初始化连接管理器
        /// </summary>
        /// <param name="options">客户端配置</param>
        public void Initialize(GameClientOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionManager));

            if (_client != null)
            {
                GameLogger.LogWarning("[ConnectionManager] Already initialized");
                return;
            }

            _options = options?.Clone() ?? new GameClientOptions();
            _client = new GameClient(_options);

            // 订阅客户端事件
            _client.OnStateChanged += HandleStateChanged;
            _client.OnError += HandleError;

            GameLogger.Log("[ConnectionManager] Initialized");
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
            RecreateClient();

            await _client.ConnectAsync();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (_client == null)
                return;
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
        /// 重新创建客户端
        /// </summary>
        private void RecreateClient()
        {
            if (_client != null)
            {
                _client.OnStateChanged -= HandleStateChanged;
                _client.OnError -= HandleError;
                _client.Dispose();
            }

            _client = new GameClient(_options);
            _client.OnStateChanged += HandleStateChanged;
            _client.OnError += HandleError;
        }

        private void HandleStateChanged(ConnectionState state)
        {
            OnStateChanged?.Invoke(state);
        }

        private void HandleError(Exception ex)
        {
            OnError?.Invoke(ex);
        }

        private void EnsureInitialized()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionManager));

            if (_client == null)
                throw new InvalidOperationException("ConnectionManager not initialized");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_client != null)
            {
                _client.OnStateChanged -= HandleStateChanged;
                _client.OnError -= HandleError;
                _client.Dispose();
                _client = null;
            }

            GameLogger.Log("[ConnectionManager] Disposed");
        }
    }
}
