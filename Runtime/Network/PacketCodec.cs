using System;
using System.Buffers.Binary;
using T2FGame.Client.Protocol;
using T2FGame.Protocol;

namespace T2FGame.Client.Network
{
    /// <summary>
    /// 数据包编解码器
    /// 协议格式: [4字节长度(大端序)] + [ExternalMessage(Protobuf)]
    /// </summary>
    public static class PacketCodec
    {
        /// <summary>
        /// 包头大小（4字节长度）
        /// </summary>
        public const int HeaderSize = 4;

        /// <summary>
        /// 最大包体大小（1MB）
        /// </summary>
        public const int MaxBodySize = 1024 * 1024;

        /// <summary>
        /// 编码消息为数据包
        /// </summary>
        public static byte[] Encode(ExternalMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var body = ProtoSerializer.Serialize(message);
            var packet = new byte[HeaderSize + body.Length];

            // 写入长度（大端序）
            BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(0, HeaderSize), body.Length);

            // 写入消息体
            Buffer.BlockCopy(body, 0, packet, HeaderSize, body.Length);

            return packet;
        }

        /// <summary>
        /// 解码数据包（从完整的数据包字节）
        /// </summary>
        public static ExternalMessage Decode(byte[] packet)
        {
            if (packet == null || packet.Length < HeaderSize)
                return null;

            var bodyLength = BinaryPrimitives.ReadInt32BigEndian(packet.AsSpan(0, HeaderSize));

            if (bodyLength is < 0 or > MaxBodySize)
                throw new InvalidOperationException($"Invalid body length: {bodyLength}");

            if (packet.Length < HeaderSize + bodyLength)
                throw new InvalidOperationException("Incomplete packet");

            return ProtoSerializer.Deserialize<ExternalMessage>(packet, HeaderSize, bodyLength);
        }

        /// <summary>
        /// 从缓冲区读取包体长度
        /// </summary>
        public static int ReadBodyLength(byte[] buffer, int offset)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset, HeaderSize));
        }
    }
}
