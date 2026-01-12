#if ENABLE_WEBSOCKET
using System;
using Pisces.Client.Utils;
using UnityWebSocket;

namespace Pisces.Client.Network.Channel
{
    /// <summary>
    /// WebSocket 通道实现
    /// 使用第三方 UnityWebSocket 插件实现
    /// 适用于 WebGL 平台或需要穿透防火墙的场景
    /// </summary>
    public class WebSocketChannel : IProtocolChannel
    {
        /// <summary>
        /// WebSocket 客户端
        /// </summary>
        private WebSocket _socket;
        
        /// <summary>
        /// 是否正在连接
        /// </summary>
        private volatile bool _isConnecting;

        /// <summary>
        /// 是否已释放
        /// </summary>
        private volatile bool _isDisposed;

        public ChannelType ChannelType => ChannelType.WebSocket;

        public bool IsConnected { get; private set; }

        public event Action<IProtocolChannel> SendMessageEvent;
        public event Action<IProtocolChannel, byte[]> ReceiveMessageEvent;
        public event Action<IProtocolChannel> DisconnectServerEvent;
        public event Action<IProtocolChannel, byte[], SendFailureReason> SendFailedEvent;

        public void OnInit()
        {
            _isDisposed = false;
            GameLogger.Log("[WebSocketChannel] 初始化完成");
        }

        public void Connect(string host, int port)
        {
            if (_isDisposed)
            {
                GameLogger.LogError("[WebSocketChannel] 通道已释放");
                return;
            }

            if (_socket != null)
            {
                if (IsConnected || _isConnecting)
                {
                    GameLogger.LogWarning("[WebSocketChannel] 已连接或正在连接中");
                    return;
                }

                // 清理旧连接
                CleanupSocket();
            }
            
            _isConnecting = true;

            try
            {
                // 构建 WebSocket URL
                // 如果 host 已经是完整的 ws:// 或 wss:// URL，直接使用
                // 否则构建 URL
                string wsUrl;
                if (
                    host.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
                    || host.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                )
                {
                    wsUrl = host;
                }
                else
                {
                    wsUrl = $"ws://{host}:{port}";
                }

                _socket = new WebSocket(wsUrl);
                _socket.OnOpen += OnOpen;
                _socket.OnClose += OnClose;
                _socket.OnMessage += OnMessage;
                _socket.OnError += OnError;
                _socket.ConnectAsync();

                GameLogger.Log($"[WebSocketChannel] 正在连接到 {wsUrl}");
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                GameLogger.LogError($"[WebSocketChannel] 连接失败: {ex.Message}");
                throw;
            }
        }

        public void Disconnect()
        {
            if (_socket == null)
                return;

            try
            {
                IsConnected = false;
                _isConnecting = false;

                if (
                    _socket.ReadyState == WebSocketState.Open
                    || _socket.ReadyState == WebSocketState.Connecting
                )
                {
                    _socket.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"[WebSocketChannel] 断开连接错误: {ex.Message}");
            }
            finally
            {
                CleanupSocket();
                DisconnectServerEvent?.Invoke(this);
                GameLogger.Log("[WebSocketChannel] 已断开连接");
            }
        }

        public bool Send(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                SendFailedEvent?.Invoke(this, data, SendFailureReason.InvalidData);
                return false;
            }

            if (!IsConnected || _socket == null)
            {
                GameLogger.LogWarning("[WebSocketChannel] 无法发送：未连接");
                SendFailedEvent?.Invoke(this, data, SendFailureReason.NotConnected);
                return false;
            }

            try
            {
                _socket.SendAsync(data);
                SendMessageEvent?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[WebSocketChannel] 发送失败: {ex.Message}");
                SendFailedEvent?.Invoke(this, data, SendFailureReason.ChannelClosed);
                return false;
            }
        }

        /// <summary>
        /// 发送文本消息
        /// </summary>
        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!IsConnected || _socket == null)
            {
                GameLogger.LogWarning("[WebSocketChannel] 无法发送：未连接");
                return;
            }

            try
            {
                _socket.SendAsync(text);
                SendMessageEvent?.Invoke(this);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[WebSocketChannel] SendText failed: {ex.Message}");
            }
        }

        private void OnOpen(object sender, OpenEventArgs e)
        {
            _isConnecting = false;
            IsConnected = true;
            GameLogger.Log("[WebSocketChannel] 已连接");
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            var wasConnected = IsConnected;
            IsConnected = false;
            _isConnecting = false;

            GameLogger.Log($"[WebSocketChannel] 已关闭: Code={e.Code}, Reason={e.Reason}");

            if (wasConnected)
            {
                DisconnectServerEvent?.Invoke(this);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _isConnecting = false;
            GameLogger.LogError($"[WebSocketChannel] 错误: {e.Message}");

            // 如果连接时出错，触发断开事件
            if (IsConnected)
            {
                IsConnected = false;
                DisconnectServerEvent?.Invoke(this);
            }
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                // 处理二进制消息
                ReceiveMessageEvent?.Invoke(this, e.RawData);
            }
            else
            {
                // 当前框架保持仅接受二进制数据
                // 如果需要处理文本消息，可以转换为字节数组
                GameLogger.LogWarning(
                    $"[WebSocketChannel] Received text message (ignored): {e.Data?.Length ?? 0} chars"
                );
            }
        }

        /// <summary>
        /// 清理 WebSocket 资源
        /// </summary>
        private void CleanupSocket()
        {
            if (_socket == null)
                return;

            _socket.OnOpen -= OnOpen;
            _socket.OnClose -= OnClose;
            _socket.OnMessage -= OnMessage;
            _socket.OnError -= OnError;
            _socket = null;
        }

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public WebSocketState? State => _socket?.ReadyState;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (disposing)
            {
                Disconnect();
            }

            GameLogger.Log("[WebSocketChannel] 已释放");
        }

        ~WebSocketChannel()
        {
            Dispose(false);
        }
    }
}
#endif
