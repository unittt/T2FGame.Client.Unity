using System;
using System.Net.Sockets;

namespace T2FGame.Client.Network.Channel
{
    /// <summary>
    /// TCP 通道实现
    /// 消息帧格式：
    /// +----------------+------------------+
    /// | Length (4字节)  | Body (N字节)      |
    /// +----------------+------------------+
    ///
    /// - Length: 消息体长度，大端序（Big-Endian）
    /// - Body: Protobuf 序列化的 ExternalMessage
    /// </summary>
    public class TcpChannel : ProtocolChannelBase
    {
        /// <summary>
        /// 消息长度字段的字节数
        /// </summary>
        private const int LengthFieldSize = 4;

        /// <summary>
        /// 接收缓冲区大小 (64KB)
        /// </summary>
        private const int TcpReceiveBufferSize = 65536;

        /// <summary>
        /// 最大消息体大小 (1MB)
        /// </summary>
        private const int MaxBodySize = 1024 * 1024;

        /// <summary>
        /// 接收缓冲区（复用以减少GC）
        /// </summary>
        private readonly byte[] _receiveBuffer = new byte[TcpReceiveBufferSize];

        /// <summary>
        /// 数据包缓冲区（处理粘包/拆包）
        /// </summary>
        private readonly PacketBuffer _packetBuffer = new PacketBuffer(TcpReceiveBufferSize);

        public override ChannelType ChannelType => ChannelType.Tcp;

        protected override SocketType Way => SocketType.Stream;

        protected override ProtocolType Protocol => ProtocolType.Tcp;

        protected override int ReceiveBufferSize => TcpReceiveBufferSize;

        /// <summary>
        /// 接收消息
        /// TCP 是流式协议，需要处理粘包/拆包问题
        /// </summary>
        protected override byte[] ReceiveMessage(Socket client)
        {
            if (client == null || !client.Connected)
                return null;

            // 检查是否有数据可读
            if (client.Available <= 0)
            {
                // 使用 Poll 检测连接状态和数据可用性
                // Poll 返回 true 且 Available 为 0 表示连接已断开
                if (client.Poll(1000, SelectMode.SelectRead) && client.Available == 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
                return null;
            }

            // 读取可用数据
            var bytesRead = client.Receive(
                _receiveBuffer,
                0,
                _receiveBuffer.Length,
                SocketFlags.None
            );

            if (bytesRead <= 0)
            {
                // 对端关闭连接
                throw new SocketException((int)SocketError.ConnectionReset);
            }

            // 写入数据包缓冲区
            _packetBuffer.Write(_receiveBuffer, 0, bytesRead);

            // 尝试解析完整的数据包
            var packets = _packetBuffer.ReadPackets();

            if (packets == null || packets.Count == 0)
                return null;

            // 如果有多个完整数据包，合并返回
            // 注意：这里返回原始字节，让上层处理解码
            // 实际上 PacketBuffer.ReadPackets 已经返回解码后的 ExternalMessage
            // 这里需要重新编码回字节数组以符合接口定义
            // 但更好的做法是修改接口或在这里直接触发事件

            // 由于 PacketBuffer 已经解码了消息，我们需要重新编码
            // 或者返回原始的完整数据包字节
            // 这里我们选择返回最后读取的原始数据，让上层使用 PacketBuffer 处理

            // 返回读取到的原始字节（包含可能的多个数据包）
            var result = new byte[bytesRead];
            Buffer.BlockCopy(_receiveBuffer, 0, result, 0, bytesRead);
            return result;
        }

        /// <summary>
        /// 获取数据包缓冲区（供上层访问解析后的消息）
        /// </summary>
        public PacketBuffer PacketBuffer => _packetBuffer;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _packetBuffer.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
