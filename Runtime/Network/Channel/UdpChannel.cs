using System;
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
        public override ChannelType ChannelType => ChannelType.Udp;

        protected override SocketType Way => SocketType.Dgram;

        protected override ProtocolType Protocol => ProtocolType.Udp;

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
                    ReceiveBuffer,
                    0,
                    ReceiveBuffer.Length,
                    SocketFlags.None
                );

                if (bytesRead <= 0)
                    return null;

                // 返回接收到的数据（复制出来，因为缓冲区会被复用）
                var result = new byte[bytesRead];
                Buffer.BlockCopy(ReceiveBuffer, 0, result, 0, bytesRead);
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

            if (data.Length > DefaultReceiveBufferSize)
            {
                GameLogger.LogError(
                    $"[UdpChannel] Data too large: {data.Length} bytes (max {DefaultReceiveBufferSize})"
                );
                return false;
            }

            return base.Send(data);
        }
    }
}
