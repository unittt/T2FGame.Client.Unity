using System;
using Google.Protobuf;

namespace Pisces.Protocol
{
    /// <summary>
    /// Protobuf 序列化器
    /// </summary>
    public static class ProtoSerializer
    {
        private static readonly byte[] _emptyArray = Array.Empty<byte>();

        /// <summary>
        /// 序列化为字节数组
        /// </summary>
        public static byte[] Serialize<T>(T value)
            where T : IMessage
        {
            return value == null ? _emptyArray : value.ToByteArray();
        }

        /// <summary>
        /// 反序列化（使用 new + MergeFrom 方式）
        /// </summary>
        public static T Deserialize<T>(ByteString data) where T : IMessage, new()
        {
            if (data == null || data.IsEmpty)
                return default;

            var obj = new T();
            obj.MergeFrom(data);

            return obj;
        }

        /// <summary>
        /// 反序列化（使用 new + MergeFrom 方式）
        /// </summary>
        public static T Deserialize<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return default;

            var obj = new T();
            obj.MergeFrom(data);

            return obj;
        }

        /// <summary>
        /// 反序列化（带偏移和长度）
        /// </summary>
        public static T Deserialize<T>(byte[] data, int offset, int count) where T : IMessage, new()
        {
            if (data == null || data.Length == 0 || count == 0)
                return default;

            var obj = new T();
            obj.MergeFrom(data, offset, count);

            return obj;
        }

        /// <summary>
        /// 反序列化（使用 MessageParser，性能更优）
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="data">字节数据</param>
        /// <param name="parser">消息解析器</param>
        /// <returns>反序列化的消息对象</returns>
        public static T Deserialize<T>(ByteString data, MessageParser<T> parser)
            where T : IMessage<T>
        {
            if (data == null || data.IsEmpty)
                return default;

            return parser.ParseFrom(data);
        }

        /// <summary>
        /// 反序列化（使用 MessageParser，性能更优）
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <param name="parser">消息解析器</param>
        /// <returns>反序列化的消息对象</returns>
        public static T Deserialize<T>(byte[] data, MessageParser<T> parser)
            where T : IMessage<T>
        {
            if (data == null || data.Length == 0)
                return default;

            return parser.ParseFrom(data);
        }

        /// <summary>
        /// 反序列化（使用 MessageParser，带偏移和长度）
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="count">字节数量</param>
        /// <param name="parser">消息解析器</param>
        /// <returns>反序列化的消息对象</returns>
        public static T Deserialize<T>(byte[] data, int offset, int count, MessageParser<T> parser)
            where T : IMessage<T>
        {
            if (data == null || data.Length == 0 || count == 0)
                return default;

            return parser.ParseFrom(data, offset, count);
        }
    }
}
