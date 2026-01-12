using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Pisces.Client.Utils;

namespace Pisces.Client.Network.Channel
{
    /// <summary>
    /// 通信协议通道的基类
    /// 提供 TCP/UDP 等基于 Socket 的通道的基础实现
    /// </summary>
    public abstract class ProtocolChannelBase : IProtocolChannel
    {
        /// <summary>
        /// 默认接收缓冲区大小
        /// </summary>
        private const int DefaultReceiveBufferSize = 65536;

        /// <summary>
        /// 线程休眠时间（毫秒）
        /// </summary>
        private const int ThreadSleepMs = 1;

        private volatile bool _isEnableThread;
        private volatile bool _isConnected;
        private volatile bool _isDisposed;

        /// <summary>
        /// 设置连接状态（供子类使用）
        /// </summary>
        protected bool IsConnectedInternal
        {
            get => _isConnected;
            set => _isConnected = value;
        }

        private Thread _sendThread;
        private Thread _receiveThread;

        /// <summary>
        /// 使用线程安全的并发队列替代 List + lock
        /// </summary>
        private readonly ConcurrentQueue<byte[]> _sendQueue = new();

        /// <summary>
        /// 用于通知发送线程有新数据
        /// </summary>
        private readonly AutoResetEvent _sendSignal = new(false);

        /// <summary>
        /// 同步上下文（用于回调到主线程）
        /// </summary>
        private SynchronizationContext _mainThreadContext;

        public abstract ChannelType ChannelType { get; }

        public bool IsConnected => _isConnected && Client is { Connected: true };

        public event Action<IProtocolChannel> SendMessageEvent;
        public event Action<IProtocolChannel, byte[]> ReceiveMessageEvent;
        public event Action<IProtocolChannel> DisconnectServerEvent;
        public event Action<IProtocolChannel, byte[], SendFailureReason> SendFailedEvent;

        /// <summary>
        /// 客户端 Socket
        /// </summary>
        protected Socket Client { get; set; }

        /// <summary>
        /// Socket 类型
        /// </summary>
        protected virtual SocketType Way => SocketType.Stream;

        /// <summary>
        /// 通信协议类型
        /// </summary>
        protected virtual ProtocolType Protocol => ProtocolType.Tcp;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        protected virtual int ReceiveBufferSize => DefaultReceiveBufferSize;

        public virtual void OnInit()
        {
            if (_isEnableThread)
                return;

            // 捕获主线程上下文
            _mainThreadContext = SynchronizationContext.Current;

            _isEnableThread = true;
            _isDisposed = false;

            _sendThread = new Thread(SendThreadLoop)
            {
                Name = $"{GetType().Name}_SendThread",
                IsBackground = true,
            };
            _sendThread.Start();

            _receiveThread = new Thread(ReceiveThreadLoop)
            {
                Name = $"{GetType().Name}_ReceiveThread",
                IsBackground = true,
            };
            _receiveThread.Start();

            GameLogger.Log($"[{ChannelType}Channel] 初始化完成");
        }

        private void ReceiveThreadLoop()
        {
            while (_isEnableThread)
            {
                try
                {
                    if (!IsConnected)
                    {
                        Thread.Sleep(ThreadSleepMs);
                        continue;
                    }

                    var data = ReceiveMessage(Client);

                    if (data is { Length: > 0 })
                    {
                        // 触发接收事件
                        InvokeOnMainThread(() => ReceiveMessageEvent?.Invoke(this, data));
                    }
                }
                catch (SocketException ex)
                {
                    GameLogger.LogError(
                        $"[{ChannelType}Channel] Socket receive error: {ex.SocketErrorCode} - {ex.Message}"
                    );
                    HandleDisconnect();
                }
                catch (ObjectDisposedException)
                {
                    // Socket 已关闭，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError(ex.Message);
                }
            }
        }

        /// <summary>
        /// 接收消息（子类实现具体的接收逻辑）
        /// </summary>
        /// <param name="client">Socket 客户端</param>
        /// <returns>接收到的原始字节数据</returns>
        protected abstract byte[] ReceiveMessage(Socket client);

        private void SendThreadLoop()
        {
            while (_isEnableThread)
            {
                try
                {
                    // 等待信号或超时
                    _sendSignal.WaitOne(100);

                    if (!IsConnected)
                        continue;

                    while (_sendQueue.TryDequeue(out var sendBuffer))
                    {
                        if (sendBuffer == null || sendBuffer.Length == 0)
                            continue;

                        var totalSent = 0;
                        var remaining = sendBuffer.Length;

                        // 确保完整发送
                        while (remaining > 0)
                        {
                            var sent = Client.Send(
                                sendBuffer,
                                totalSent,
                                remaining,
                                SocketFlags.None
                            );
                            if (sent <= 0)
                            {
                                throw new SocketException((int)SocketError.ConnectionReset);
                            }
                            totalSent += sent;
                            remaining -= sent;
                        }

                        // 触发发送成功事件
                        InvokeOnMainThread(() => SendMessageEvent?.Invoke(this));
                    }
                }
                catch (SocketException ex)
                {
                    GameLogger.LogError(
                        $"[{ChannelType}Channel] Socket send error: {ex.SocketErrorCode} - {ex.Message}"
                    );
                    HandleDisconnect();
                }
                catch (ObjectDisposedException)
                {
                    // Socket 已关闭，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError(ex.Message);
                }
            }
        }

        public virtual void Connect(string host, int port)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ProtocolChannelBase));
            }

            if (IsConnected)
            {
                GameLogger.LogWarning($"[{ChannelType}Channel] 已连接");
                return;
            }

            try
            {
                Client?.Close();

                // 解析主机地址（支持域名和 IP 地址）
                IPAddress ipAddress;
                if (!IPAddress.TryParse(host, out ipAddress))
                {
                    // 不是 IP 地址，尝试 DNS 解析
                    var addresses = Dns.GetHostAddresses(host);
                    if (addresses == null || addresses.Length == 0)
                    {
                        throw new ArgumentException($"无法解析主机地址: {host}");
                    }

                    // 优先使用 IPv4 地址
                    ipAddress = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                                ?? addresses[0];
                }

                var addressFamily = ipAddress.AddressFamily;
                Client = new Socket(addressFamily, Way, Protocol)
                {
                    ReceiveBufferSize = ReceiveBufferSize,
                    SendBufferSize = ReceiveBufferSize,
                    NoDelay = true, // 禁用 Nagle 算法，减少延迟
                };

                var endPoint = new IPEndPoint(ipAddress, port);
                Client.Connect(endPoint);
                _isConnected = true;

                GameLogger.Log($"[{ChannelType}Channel] 已连接到 {host}:{port}");
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[{ChannelType}Channel] 连接失败: {ex.Message}");
                _isConnected = false;
                throw;
            }
        }

        public virtual void Disconnect()
        {
            if (Client == null)
                return;

            try
            {
                _isConnected = false;

                if (Client.Connected)
                {
                    Client.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {
                // 忽略 Shutdown 异常
            }
            finally
            {
                Client.Close();
                Client = null;

                InvokeOnMainThread(() => DisconnectServerEvent?.Invoke(this));
                GameLogger.Log($"[{ChannelType}Channel] 已断开连接");
            }
        }

        public virtual bool Send(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                GameLogger.LogWarning($"[{ChannelType}Channel] 无法发送：数据无效");
                InvokeOnMainThread(() => SendFailedEvent?.Invoke(this, data, SendFailureReason.InvalidData));
                return false;
            }

            if (_isDisposed)
            {
                GameLogger.LogWarning($"[{ChannelType}Channel] 无法发送：通道已关闭");
                InvokeOnMainThread(() => SendFailedEvent?.Invoke(this, data, SendFailureReason.ChannelClosed));
                return false;
            }

            if (!IsConnected)
            {
                GameLogger.LogWarning($"[{ChannelType}Channel] 无法发送：未连接");
                InvokeOnMainThread(() => SendFailedEvent?.Invoke(this, data, SendFailureReason.NotConnected));
                return false;
            }

            _sendQueue.Enqueue(data);
            _sendSignal.Set(); // 通知发送线程
            return true;
        }

        /// <summary>
        /// 处理断开连接
        /// </summary>
        private void HandleDisconnect()
        {
            if (!_isConnected)
                return;

            _isConnected = false;
            InvokeOnMainThread(() => DisconnectServerEvent?.Invoke(this));
        }

        /// <summary>
        /// 在主线程上执行回调
        /// </summary>
        private void InvokeOnMainThread(Action action)
        {
            if (action == null)
                return;

            if (_mainThreadContext != null)
            {
                _mainThreadContext.Post(_ => action(), null);
            }
            else
            {
                // 如果没有同步上下文，直接执行
                action();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _isEnableThread = false;
            _sendSignal.Set(); // 唤醒发送线程以退出

            if (disposing)
            {
                Disconnect();

                // 等待线程结束
                _sendThread?.Join(1000);
                _receiveThread?.Join(1000);

                _sendSignal.Dispose();

                // 清空队列
                while (_sendQueue.TryDequeue(out _)) { }
            }

            GameLogger.Log($"[{ChannelType}Channel] 已释放");
        }

        ~ProtocolChannelBase()
        {
            Dispose(false);
        }
    }
}
