using System;
using System.Collections.Generic;
using Google.Protobuf;
using Pisces.Protocol;
using Pisces.Client.Utils;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 对 ExternalMessage 的封装，提供安全的访问器与缓存。
    /// </summary>
    public sealed class ResponseMessage :  IPoolable
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

        /// <summary>
        /// 获取错误消息。如果消息为空或无错误则返回空字符串。
        /// </summary>
        public string ErrorMessage => _message?.ValidMsg ?? string.Empty;

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
        public T GetValue<T>()
            where T : IMessage, new()
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
        /// 获取整数值。
        /// </summary>
        /// <returns>从消息体中解析出的整数。如果数据为空则返回 0。</returns>
        public int GetInt() => GetValue<IntValue>()?.Value ?? 0;

        /// <summary>
        /// 获取整数列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的整数只读列表。</returns>
        public IReadOnlyList<int> ListInt() => GetValue<IntValueList>()?.Values ?? (IReadOnlyList<int>)Array.Empty<int>();

        /// <summary>
        /// 获取长整数值。
        /// </summary>
        /// <returns>从消息体中解析出的长整数。如果数据为空则返回 0。</returns>
        public long GetLong() => GetValue<LongValue>()?.Value ?? 0L;

        /// <summary>
        /// 获取长整数列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的长整数只读列表。</returns>
        public IReadOnlyList<long> ListLong() => GetValue<LongValueList>()?.Values ?? (IReadOnlyList<long>)Array.Empty<long>();

        /// <summary>
        /// 获取字符串值。
        /// </summary>
        /// <returns>从消息体中解析出的字符串。如果数据为空则返回空字符串。</returns>
        public string GetString() => GetValue<StringValue>()?.Value ?? string.Empty;

        /// <summary>
        /// 获取字符串列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的字符串只读列表。</returns>
        public IReadOnlyList<string> ListString() => GetValue<StringValueList>()?.Values ?? (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>
        /// 获取布尔值。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值。如果数据为空则返回 false。</returns>
        public bool GetBool() => GetValue<BoolValue>()?.Value ?? false;

        /// <summary>
        /// 获取布尔值列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值只读列表。</returns>
        public IReadOnlyList<bool> ListBool() => GetValue<BoolValueList>()?.Values ?? (IReadOnlyList<bool>)Array.Empty<bool>();

        #region Vector 类型支持

        /// <summary>
        /// 获取 Vector2 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2。如果数据为空则返回 Vector2.zero。</returns>
        public UnityEngine.Vector2 GetVector2() => GetValue<Vector2>() ?? UnityEngine.Vector2.zero;

        /// <summary>
        /// 获取 Vector2Int 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2Int。如果数据为空则返回 Vector2Int.zero。</returns>
        public UnityEngine.Vector2Int GetVector2Int() => GetValue<Vector2Int>() ?? UnityEngine.Vector2Int.zero;

        /// <summary>
        /// 获取 Vector3 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3。如果数据为空则返回 Vector3.zero。</returns>
        public UnityEngine.Vector3 GetVector3() => GetValue<Vector3>() ?? UnityEngine.Vector3.zero;

        /// <summary>
        /// 获取 Vector3Int 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3Int。如果数据为空则返回 Vector3Int.zero。</returns>
        public UnityEngine.Vector3Int GetVector3Int() => GetValue<Vector3Int>() ?? UnityEngine.Vector3Int.zero;

        /// <summary>
        /// 获取 Vector2 列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2 只读列表。</returns>
        public IReadOnlyList<Vector2> ListVector2() => GetValue<Vector2List>()?.Values ?? (IReadOnlyList<Vector2>)Array.Empty<Vector2>();

        /// <summary>
        /// 获取 Vector2Int 列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2Int 只读列表。</returns>
        public IReadOnlyList<Vector2Int> ListVector2Int() => GetValue<Vector2IntList>()?.Values ?? (IReadOnlyList<Vector2Int>)Array.Empty<Vector2Int>();

        /// <summary>
        /// 获取 Vector3 列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3 只读列表。</returns>
        public IReadOnlyList<Vector3> ListVector3() => GetValue<Vector3List>()?.Values ?? (IReadOnlyList<Vector3>)Array.Empty<Vector3>();

        /// <summary>
        /// 获取 Vector3Int 列表（零 GC）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3Int 只读列表。</returns>
        public IReadOnlyList<Vector3Int> ListVector3Int() => GetValue<Vector3IntList>()?.Values ?? (IReadOnlyList<Vector3Int>)Array.Empty<Vector3Int>();

        #endregion
    }
}
