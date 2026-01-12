using System;
using System.Net.Sockets;

namespace Pisces.Client.Network.Channel
{
    /// <summary>
    /// TCP 通道实现
    /// 负责 TCP 连接的建立、数据收发
    /// 粘包/拆包处理由上层 GameClient 负责
    /// </summary>
    public class TcpChannel : ProtocolChannelBase
    {
        /// <summary>
        /// 接收缓冲区大小 (64KB)
        /// </summary>
        private const int TcpReceiveBufferSize = 65536;

        /// <summary>
        /// 接收缓冲区（复用以减少GC）
        /// </summary>
        private readonly byte[] _receiveBuffer = new byte[TcpReceiveBufferSize];

        public override ChannelType ChannelType => ChannelType.Tcp;

        protected override SocketType Way => SocketType.Stream;

        protected override ProtocolType Protocol => ProtocolType.Tcp;

        protected override int ReceiveBufferSize => TcpReceiveBufferSize;

        /// <summary>
        /// 接收消息
        /// 返回原始字节数据，粘包/拆包由上层处理
        /// </summary>
        protected override byte[] ReceiveMessage(Socket client)
        {
            if (client is not { Connected: true })
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

            // 返回读取到的原始字节
            var result = new byte[bytesRead];
            Buffer.BlockCopy(_receiveBuffer, 0, result, 0, bytesRead);
            return result;
        }
    }
}
