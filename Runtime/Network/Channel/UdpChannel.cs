using System;
using System.Net;
using System.Net.Sockets;
using T2FGame.Client.Utils;

namespace T2FGame.Client.Network.Channel
{
    /// <summary>
    /// UDP 通道实现
    /// UDP 是无连接的数据报协议，每个数据报是独立的，不需要处理粘包问题
    /// 适用于对实时性要求高、可以容忍丢包的场景（如位置同步）
    ///
    /// 消息格式：直接发送完整的数据包，无需长度前缀
    /// （因为 UDP 数据报有边界，每个 Receive 返回一个完整的数据报）
    /// </summary>
    public class UdpChannel : ProtocolChannelBase
    {
        /// <summary>
        /// UDP 接收缓冲区大小 (64KB - UDP 最大数据报约 65507 字节)
        /// </summary>
        private const int UdpReceiveBufferSize = 65536;

        /// <summary>
        /// 接收缓冲区（复用以减少GC）
        /// </summary>
        private readonly byte[] _receiveBuffer = new byte[UdpReceiveBufferSize];

        /// <summary>
        /// 服务器端点
        /// </summary>
        private EndPoint _serverEndPoint;

        /// <summary>
        /// 是否已绑定
        /// </summary>
        private bool _isBound;

        public override ChannelType ChannelType => ChannelType.Udp;

        protected override SocketType Way => SocketType.Dgram;

        protected override ProtocolType Protocol => ProtocolType.Udp;

        protected override int ReceiveBufferSize => UdpReceiveBufferSize;

        public override void Connect(string host, int port)
        {
            if (Client != null && _isBound)
            {
                GameLogger.LogWarning("[UdpChannel] 已连接");
                return;
            }

            try
            {
                // 创建 UDP Socket
                Client?.Close();
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp
                )
                {
                    ReceiveBufferSize = UdpReceiveBufferSize,
                    SendBufferSize = UdpReceiveBufferSize,
                };

                // 保存服务器端点
                _serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

                // UDP 使用 Connect 可以限定只接收来自指定端点的数据
                // 同时允许使用 Send 而不是 SendTo
                socket.Connect(_serverEndPoint);

                // 通过反射设置基类的 Client（因为基类 Client 是 protected set）
                // 这里我们需要直接调用基类的 Connect 方法来设置
                // 但由于基类 Connect 会创建新 Socket，我们需要重写整个逻辑

                // 绑定本地端口（让系统自动分配）
                // socket.Bind(new IPEndPoint(IPAddress.Any, 0));

                _isBound = true;

                // 使用基类的连接方法（会设置 Client 和 _isConnected）
                base.Connect(host, port);

                GameLogger.Log($"[UdpChannel] 已连接到 {host}:{port}");
            }
            catch (Exception ex)
            {
                _isBound = false;
                GameLogger.LogError($"[UdpChannel] 连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 接收消息
        /// UDP 数据报有边界，每次 Receive 返回一个完整的数据报
        /// </summary>
        protected override byte[] ReceiveMessage(Socket client)
        {
            if (client == null)
                return null;

            try
            {
                // 检查是否有数据可读
                if (client.Available <= 0)
                {
                    // 使用 Poll 等待数据，超时 100ms
                    if (!client.Poll(100000, SelectMode.SelectRead))
                    {
                        return null;
                    }

                    // 再次检查
                    if (client.Available <= 0)
                        return null;
                }

                // UDP 接收 - 每次接收一个完整的数据报
                var bytesRead = client.Receive(
                    _receiveBuffer,
                    0,
                    _receiveBuffer.Length,
                    SocketFlags.None
                );

                if (bytesRead <= 0)
                    return null;

                // 返回接收到的数据（复制出来，因为缓冲区会被复用）
                var result = new byte[bytesRead];
                Buffer.BlockCopy(_receiveBuffer, 0, result, 0, bytesRead);
                return result;
            }
            catch (SocketException ex)
            {
                // UDP 可能会收到 ICMP 错误（如端口不可达）
                // 错误码 10054 (WSAECONNRESET) 在 UDP 中表示目标不可达
                if (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    GameLogger.LogWarning($"[UdpChannel] 收到 ICMP 错误: {ex.Message}");
                    return null; // 忽略，继续接收
                }
                throw;
            }
        }

        /// <summary>
        /// 发送数据（UDP 无连接，直接发送）
        /// </summary>
        public override void Send(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (data.Length > UdpReceiveBufferSize)
            {
                GameLogger.LogError(
                    $"[UdpChannel] Data too large: {data.Length} bytes (max {UdpReceiveBufferSize})"
                );
                return;
            }

            // UDP 可以直接发送，不需要排队（无连接、无序）
            // 但为了保持一致性，我们仍使用基类的队列机制
            base.Send(data);
        }

        public override void Disconnect()
        {
            _isBound = false;
            _serverEndPoint = null;
            base.Disconnect();
        }

        protected override void Dispose(bool disposing)
        {
            _isBound = false;
            _serverEndPoint = null;
            base.Dispose(disposing);
        }
    }
}
