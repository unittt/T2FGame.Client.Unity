using System;
using System.Buffers.Binary;
using Pisces.Client.Sdk;
using Pisces.Protocol;

namespace Pisces.Client.Network.Core
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
            if (message == null) throw new ArgumentNullException(nameof(message));

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
                PiscesClientCode.DeserializationError.ThrowIfFailed($"Invalid body length: {bodyLength}");


            if (packet.Length < HeaderSize + bodyLength)
                PiscesClientCode.DeserializationError.ThrowIfFailed("Incomplete packet");

            return DecodeBody(packet, HeaderSize, bodyLength);
        }

        /// <summary>
        /// 从缓冲区解码消息体（不包含长度头）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">消息体起始偏移</param>
        /// <param name="bodyLength">消息体长度</param>
        /// <returns>解码后的消息</returns>
        public static ExternalMessage DecodeBody(byte[] buffer, int offset, int bodyLength)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (bodyLength is < 0 or > MaxBodySize)
                PiscesClientCode.DeserializationError.ThrowIfFailed($"Invalid body length: {bodyLength}");

            if (offset < 0 || offset + bodyLength > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return ProtoSerializer.Deserialize(buffer, offset, bodyLength, ExternalMessage.Parser);
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
