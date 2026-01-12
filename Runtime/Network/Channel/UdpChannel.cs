using System;
using System.Net;
using System.Net.Sockets;
using Pisces.Client.Utils;

namespace Pisces.Client.Network.Channel
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

        public override ChannelType ChannelType => ChannelType.Udp;

        protected override SocketType Way => SocketType.Dgram;

        protected override ProtocolType Protocol => ProtocolType.Udp;

        protected override int ReceiveBufferSize => UdpReceiveBufferSize;

        public override void Connect(string host, int port)
        {
            if (IsConnected)
            {
                GameLogger.LogWarning("[UdpChannel] 已连接");
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
                Client = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp)
                {
                    ReceiveBufferSize = UdpReceiveBufferSize,
                    SendBufferSize = UdpReceiveBufferSize,
                };

                // UDP 使用 Connect 可以限定只接收来自指定端点的数据
                // 同时允许使用 Send 而不是 SendTo
                var endPoint = new IPEndPoint(ipAddress, port);
                Client.Connect(endPoint);
                IsConnectedInternal = true;

                GameLogger.Log($"[UdpChannel] 已连接到 {host}:{port}");
            }
            catch (Exception ex)
            {
                IsConnectedInternal = false;
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
        /// 发送数据
        /// </summary>
        public override bool Send(byte[] data)
        {
            if (data == null || data.Length == 0)
                return false;

            if (data.Length > UdpReceiveBufferSize)
            {
                GameLogger.LogError(
                    $"[UdpChannel] Data too large: {data.Length} bytes (max {UdpReceiveBufferSize})"
                );
                return false;
            }

            return base.Send(data);
        }
    }
}
