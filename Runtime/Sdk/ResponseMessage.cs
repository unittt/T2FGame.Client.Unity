using System;
using Google.Protobuf;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 对 ExternalMessage 的封装，提供安全的访问器与缓存。
    /// </summary>
    public sealed partial class ResponseMessage : IPoolable
    {
        private ExternalMessage _message;

        // 反序列化缓存，避免重复解析同一响应
        private object _cachedValue;
        private Type _cachedType;

        /// <summary>
        /// 获取合并后的命令码。如果消息为空则返回 0。
        /// </summary>
        public int CmdMerge => _message?.CmdMerge ?? 0;

        /// <summary>
        /// 获取消息 ID。如果消息为空则返回 0。
        /// </summary>
        public int MsgId => _message?.MsgId ?? 0;

        /// <summary>
        /// 请求命令类型
        /// </summary>
        public MessageType MessageType { get; private set; }

        /// <summary>
        /// 获取响应状态码。如果消息为空则返回 0。
        /// </summary>
        public int ResponseStatus { get; private set; }

        /// <summary>
        /// 判断是否存在错误（即响应状态码是否不为 0）。
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// 判断请求是否成功（即响应状态码是否为 0）。
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 获取错误消息。如果消息为空或无错误则返回空字符串。
        /// </summary>
        public string ErrorMessage => _message?.ValidMsg ?? string.Empty;

        /// <summary>
        /// 获取原始数据
        /// </summary>
        internal ByteString RawData => _message?.Data;

        public void Initialize(ExternalMessage message)
        {
            _message = message;
            MessageType = _message.MessageType;
            ResponseStatus = _message?.ResponseStatus ?? 0;
            HasError = ResponseStatus != 0;
            Success = ResponseStatus == 0;
        }

        /// <summary>
        /// 重置响应消息状态（用于对象池回收）
        /// </summary>
        public void Reset()
        {
            _message = null;
            MessageType = MessageType.Heartbeat;
            ResponseStatus = 0;
            HasError = false;
            Success = false;

            // 清理缓存
            _cachedValue = null;
            _cachedType = null;
        }

        /// <summary>
        /// IPoolable: 从池中取出时调用
        /// </summary>
        public void OnSpawn()
        {
            // 对象从池中取出时不需要特殊处理，Initialize 会设置所有属性
        }

        /// <summary>
        /// IPoolable: 归还到池中时调用
        /// </summary>
        public void OnDespawn()
        {
            Reset();
        }

        /// <summary>
        /// 获取值。支持缓存机制，同一类型多次调用不会重复反序列化。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <returns>反序列化后的对象</returns>
        public T GetValue<T>() where T : IMessage, new()
        {
            if (_message == null || _message.Data.IsEmpty)
            {
                return default;
            }

            // 检查缓存：如果已经反序列化过相同类型，直接返回
            if (_cachedValue != null && _cachedType == typeof(T))
            {
                return (T)_cachedValue;
            }

            // 反序列化并缓存结果
            var value = ProtoSerializer.Deserialize<T>(_message.Data);
            _cachedValue = value;
            _cachedType = typeof(T);

            return value;
        }

        /// <summary>
        /// 获取值（使用 MessageParser，性能更优）。支持缓存机制，同一类型多次调用不会重复反序列化。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="parser">消息解析器，通常使用 YourMessage.Parser</param>
        /// <returns>反序列化后的对象</returns>
        public T GetValue<T>(MessageParser<T> parser) where T : IMessage<T>
        {
            if (_message == null || _message.Data.IsEmpty)
            {
                return default;
            }

            // 检查缓存：如果已经反序列化过相同类型，直接返回
            if (_cachedValue != null && _cachedType == typeof(T))
            {
                return (T)_cachedValue;
            }

            // 使用 MessageParser 反序列化并缓存结果
            var value = ProtoSerializer.Deserialize(_message.Data, parser);
            _cachedValue = value;
            _cachedType = typeof(T);

            return value;
        }
    }
}
