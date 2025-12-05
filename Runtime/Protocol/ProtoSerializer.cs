using System;
using Google.Protobuf;


namespace T2FGame.Client.Protocol
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
        public static byte[] Serialize<T>(T value) where T : IMessage
        {
            return value == null ? _emptyArray : value.ToByteArray();
        }

        /// <summary>
        /// 反序列化
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
        /// 反序列化
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
    }
}
