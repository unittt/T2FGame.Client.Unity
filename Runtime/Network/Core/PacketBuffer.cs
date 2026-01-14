using System;
using System.Collections.Generic;
using Pisces.Protocol;

namespace Pisces.Client.Network.Core
{
    /// <summary>
    /// 数据包缓冲区（处理粘包/拆包）
    /// 优化：使用较小的初始大小，支持自动收缩以节省内存
    /// </summary>
    public sealed class PacketBuffer
    {
        /// <summary>
        /// 默认初始大小（4KB，可满足大多数消息需求）
        /// </summary>
        private const int DefaultInitialSize = 4096;

        /// <summary>
        /// 默认收缩阈值（64KB）
        /// </summary>
        private const int DefaultShrinkThreshold = 65536;

        /// <summary>
        /// 收缩使用率阈值（低于此比例时触发收缩）
        /// </summary>
        private const float ShrinkUsageThreshold = 0.25f;

        private byte[] _buffer;
        private int _writePos;
        private readonly int _initialSize;
        private readonly int _shrinkThreshold;
        private readonly List<ExternalMessage> _packetsCache = new();

        /// <summary>
        /// 缓冲区中的数据长度
        /// </summary>
        public int Length => _writePos;

        /// <summary>
        /// 缓冲区容量
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// 创建数据包缓冲区
        /// </summary>
        /// <param name="initialSize">初始大小（字节），默认 4KB</param>
        /// <param name="shrinkThreshold">收缩阈值（字节），当容量超过此值且使用率低时自动收缩</param>
        public PacketBuffer(int initialSize = DefaultInitialSize, int shrinkThreshold = DefaultShrinkThreshold)
        {
            _initialSize = Math.Max(initialSize, 1024); // 最小 1KB
            _shrinkThreshold = Math.Max(shrinkThreshold, _initialSize * 2);
            _buffer = new byte[_initialSize];
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
        /// 写入数据（ArraySegment 重载）
        /// </summary>
        public void Write(ArraySegment<byte> data)
        {
            if (data.Count <= 0)
                return;

            EnsureCapacity(data.Count);
            Buffer.BlockCopy(data.Array, data.Offset, _buffer, _writePos, data.Count);
            _writePos += data.Count;
        }

        /// <summary>
        /// 尝试读取完整的数据包
        /// 注意：返回的列表会在下次调用时被清空，调用者应立即处理
        /// </summary>
        /// <returns>读取到的完整消息列表</returns>
        public List<ExternalMessage> ReadPackets()
        {
            _packetsCache.Clear();
            var readPos = 0;

            while (readPos + PacketCodec.HeaderSize <= _writePos)
            {
                // 读取长度头
                var bodyLength = PacketCodec.ReadBodyLength(_buffer, readPos);

                // 检查数据是否完整
                var packetLength = PacketCodec.HeaderSize + bodyLength;
                if (readPos + packetLength > _writePos)
                {
                    break; // 数据不完整，等待更多数据
                }

                // 反序列化消息
                var message = PacketCodec.DecodeBody(_buffer, readPos + PacketCodec.HeaderSize, bodyLength);

                if (message != null)
                {
                    _packetsCache.Add(message);
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

                // 尝试收缩缓冲区
                TryShrink();
            }

            return _packetsCache;
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            _writePos = 0;

            // 如果缓冲区过大，重置为初始大小
            if (_buffer.Length > _shrinkThreshold)
            {
                _buffer = new byte[_initialSize];
            }
        }

        /// <summary>
        /// 尝试收缩缓冲区以节省内存
        /// 当容量超过阈值且使用率低于 25% 时触发
        /// </summary>
        private void TryShrink()
        {
            // 只在容量超过阈值时考虑收缩
            if (_buffer.Length <= _shrinkThreshold)
                return;

            // 计算使用率
            float usage = (float)_writePos / _buffer.Length;
            if (usage >= ShrinkUsageThreshold)
                return;

            // 计算新的大小（当前数据量的 2 倍，但不小于初始大小）
            var newSize = Math.Max(_writePos * 2, _initialSize);

            // 如果新大小明显小于当前大小，则收缩
            if (newSize < _buffer.Length / 2)
            {
                var newBuffer = new byte[newSize];
                if (_writePos > 0)
                {
                    Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _writePos);
                }
                _buffer = newBuffer;
            }
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
