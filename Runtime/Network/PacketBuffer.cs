using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using T2FGame.Client.Protocol;
using T2FGame.Protocol;

namespace T2FGame.Client.Network
{
    /// <summary>
    /// 数据包缓冲区（处理粘包/拆包）
    /// </summary>
    public sealed class PacketBuffer
    {
        private byte[] _buffer;
        private int _writePos;

        /// <summary>
        /// 缓冲区中的数据长度
        /// </summary>
        public int Length => _writePos;

        /// <summary>
        /// 缓冲区容量
        /// </summary>
        public int Capacity => _buffer.Length;

        public PacketBuffer(int initialSize = 65536)
        {
            _buffer = new byte[initialSize];
            _writePos = 0;
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        public void Write(byte[] data, int offset, int count)
        {
            if (count <= 0)
                return;

            EnsureCapacity(count);
            Buffer.BlockCopy(data, offset, _buffer, _writePos, count);
            _writePos += count;
        }

        /// <summary>
        /// 尝试读取完整的数据包
        /// </summary>
        /// <returns>读取到的完整消息列表</returns>
        public List<ExternalMessage> ReadPackets()
        {
            var packets = new List<ExternalMessage>();
            var readPos = 0;

            while (readPos + PacketCodec.HeaderSize <= _writePos)
            {
                // 读取长度头（大端序）
                var bodyLength = BinaryPrimitives.ReadInt32BigEndian(
                    _buffer.AsSpan(readPos, PacketCodec.HeaderSize)
                );

                // 验证长度
                if (bodyLength is < 0 or > PacketCodec.MaxBodySize)
                {
                    throw new InvalidOperationException($"数据包长度无效: {bodyLength}");
                }

                // 检查数据是否完整
                var packetLength = PacketCodec.HeaderSize + bodyLength;
                if (readPos + packetLength > _writePos)
                {
                    break; // 数据不完整，等待更多数据
                }

                // 反序列化消息
                var message = ProtoSerializer.Deserialize<ExternalMessage>(
                    _buffer,
                    readPos + PacketCodec.HeaderSize,
                    bodyLength
                );

                if (message != null)
                {
                    packets.Add(message);
                }

                readPos += packetLength;
            }

            // 移动剩余数据到缓冲区开头
            if (readPos > 0)
            {
                var remaining = _writePos - readPos;
                if (remaining > 0)
                {
                    Buffer.BlockCopy(_buffer, readPos, _buffer, 0, remaining);
                }
                _writePos = remaining;
            }

            return packets;
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            _writePos = 0;
        }

        /// <summary>
        /// 确保有足够的容量
        /// </summary>
        private void EnsureCapacity(int additionalSize)
        {
            var required = _writePos + additionalSize;
            if (required <= _buffer.Length)
                return;

            // 扩容为 2 倍或所需大小
            var newSize = Math.Max(_buffer.Length * 2, required);
            var newBuffer = new byte[newSize];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _writePos);
            _buffer = newBuffer;
        }
    }
}
