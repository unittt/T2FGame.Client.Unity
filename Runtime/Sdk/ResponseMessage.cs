using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using T2FGame.Client.Protocol;
using T2FGame.Client.Utils;
using T2FGame.Protocol;

namespace T2FGame.Client.Sdk
{
    /// <summary>
    /// 对 ExternalMessage 的封装，提供安全的访问器与缓存。
    /// </summary>
    public sealed class ResponseMessage : IPoolable
    {
        private ExternalMessage _message;

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
        public CommandType CommandType { get; private set; }

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

        public void Initialize(ExternalMessage message)
        {
            _message = message;
            CommandType = _message.CmdCode == 0 ? CommandType.Heartbeat : CommandType.Business;

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
            CommandType = CommandType.Business;
            ResponseStatus = 0;
            HasError = false;
            Success = false;
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
        ///  获取值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
            where T : IMessage, new()
        {
            if (_message == null || _message.Data.IsEmpty)
            {
                return default;
            }

            return ProtoSerializer.Deserialize<T>(_message.Data);
        }

        /// <summary>
        /// 获取整数值。
        /// </summary>
        /// <returns>从消息体中解析出的整数。</returns>
        public int GetInt() => GetValue<IntValue>().Value;

        /// <summary>
        /// 获取整数列表。
        /// </summary>
        /// <returns>从消息体中解析出的整数集合。</returns>
        public List<int> ListInt()
        {
            return GetValue<IntValueList>().Values.ToList();
        }

        /// <summary>
        /// 获取长整数值。
        /// </summary>
        /// <returns>从消息体中解析出的长整数。</returns>
        public long GetLong() => GetValue<LongValue>().Value;

        /// <summary>
        /// 获取长整数列表。
        /// </summary>
        /// <returns>从消息体中解析出的长整数集合。</returns>
        public List<long> ListLong()
        {
            return GetValue<LongValueList>().Values.ToList();
        }

        /// <summary>
        /// 获取字符串值。
        /// </summary>
        /// <returns>从消息体中解析出的字符串。</returns>
        public string GetString() => GetValue<StringValue>().Value;

        /// <summary>
        /// 获取字符串列表。
        /// </summary>
        /// <returns>从消息体中解析出的字符串集合。</returns>
        public List<string> ListString()
        {
            return GetValue<StringValueList>().Values.ToList();
        }

        /// <summary>
        /// 获取布尔值。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值。</returns>
        public bool GetBool() => GetValue<BoolValue>().Value;

        /// <summary>
        /// 获取布尔值列表。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值集合。</returns>
        public List<bool> ListBool()
        {
            return GetValue<BoolValueList>().Values.ToList();
        }
    }
}
