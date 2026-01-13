using System;
using System.Collections.Generic;
using Google.Protobuf;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// ResponseMessage 便捷访问器扩展
    /// </summary>
    public sealed partial class ResponseMessage
    {
        #region 基础类型访问器

        /// <summary>
        /// 获取整数值。
        /// </summary>
        /// <returns>从消息体中解析出的整数。如果数据为空则返回 0。</returns>
        public int GetInt() => GetValue(IntValue.Parser)?.Value ?? 0;

        /// <summary>
        /// 获取长整数值。
        /// </summary>
        /// <returns>从消息体中解析出的长整数。如果数据为空则返回 0。</returns>
        public long GetLong() => GetValue(LongValue.Parser)?.Value ?? 0L;

        /// <summary>
        /// 获取字符串值。
        /// </summary>
        /// <returns>从消息体中解析出的字符串。如果数据为空则返回空字符串。</returns>
        public string GetString() => GetValue(StringValue.Parser)?.Value ?? string.Empty;

        /// <summary>
        /// 获取布尔值。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值。如果数据为空则返回 false。</returns>
        public bool GetBool() => GetValue(BoolValue.Parser)?.Value ?? false;

        #endregion

        #region 基础列表访问器

        /// <summary>
        /// 获取整数列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的整数只读列表。</returns>
        public IReadOnlyList<int> ListInt() => GetValue(IntValueList.Parser)?.Values ?? (IReadOnlyList<int>)Array.Empty<int>();

        /// <summary>
        /// 获取长整数列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的长整数只读列表。</returns>
        public IReadOnlyList<long> ListLong() => GetValue(LongValueList.Parser)?.Values ?? (IReadOnlyList<long>)Array.Empty<long>();

        /// <summary>
        /// 获取字符串列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的字符串只读列表。</returns>
        public IReadOnlyList<string> ListString() => GetValue(StringValueList.Parser)?.Values ?? (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>
        /// 获取布尔值列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的布尔值只读列表。</returns>
        public IReadOnlyList<bool> ListBool() => GetValue(BoolValueList.Parser)?.Values ?? (IReadOnlyList<bool>)Array.Empty<bool>();

        #endregion

        #region Vector 类型访问器

        /// <summary>
        /// 获取 Vector2 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2。如果数据为空则返回 Vector2.zero。</returns>
        public UnityEngine.Vector2 GetVector2() => GetValue(Vector2.Parser) ?? UnityEngine.Vector2.zero;

        /// <summary>
        /// 获取 Vector3 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3。如果数据为空则返回 Vector3.zero。</returns>
        public UnityEngine.Vector3 GetVector3() => GetValue(Vector3.Parser) ?? UnityEngine.Vector3.zero;

        /// <summary>
        /// 获取 Vector2Int 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2Int。如果数据为空则返回 Vector2Int.zero。</returns>
        public UnityEngine.Vector2Int GetVector2Int() =>
            GetValue(Vector2Int.Parser) ?? UnityEngine.Vector2Int.zero;

        /// <summary>
        /// 获取 Vector3Int 值。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3Int。如果数据为空则返回 Vector3Int.zero。</returns>
        public UnityEngine.Vector3Int GetVector3Int() => GetValue(Vector3Int.Parser) ?? UnityEngine.Vector3Int.zero;

        #endregion

        #region Vector 列表访问器

        /// <summary>
        /// 获取 Vector2 列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2 只读列表。</returns>
        public IReadOnlyList<Vector2> ListVector2() => GetValue(Vector2List.Parser)?.Values ?? (IReadOnlyList<Vector2>)Array.Empty<Vector2>();

        /// <summary>
        /// 获取 Vector3 列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3 只读列表。</returns>
        public IReadOnlyList<Vector3> ListVector3() => GetValue(Vector3List.Parser)?.Values ?? (IReadOnlyList<Vector3>)Array.Empty<Vector3>();

        /// <summary>
        /// 获取 Vector2Int 列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector2Int 只读列表。</returns>
        public IReadOnlyList<Vector2Int> ListVector2Int() => GetValue(Vector2IntList.Parser)?.Values
                                                             ?? (IReadOnlyList<Vector2Int>)Array.Empty<Vector2Int>();

        /// <summary>
        /// 获取 Vector3Int 列表（利用缓存，后续调用避免重复反序列化）。
        /// </summary>
        /// <returns>从消息体中解析出的 Vector3Int 只读列表。</returns>
        public IReadOnlyList<Vector3Int> ListVector3Int() =>
            GetValue(Vector3IntList.Parser)?.Values
            ?? (IReadOnlyList<Vector3Int>)Array.Empty<Vector3Int>();

        #endregion

        #region 泛型列表访问器

        /// <summary>
        /// 获取 Protobuf 消息列表，填充到指定结果列表中（复用传入的列表容器）。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="result">用于接收结果的列表</param>
        public void GetList<T>(List<T> result) where T : IMessage, new()
        {
            var list = GetValue(ByteValueList.Parser);
            list?.ToList(result);
        }

        #endregion

        #region 字典访问器

        /// <summary>
        /// 获取 int 键字典，填充到指定结果字典中（复用传入的字典容器）。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void GetDictionary<T>(Dictionary<int, T> result) where T : IMessage, new()
        {
            var map = GetValue(IntKeyMap.Parser);
            map?.ToDictionary(result);
        }

        /// <summary>
        /// 获取 long 键字典，填充到指定结果字典中（复用传入的字典容器）。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void GetDictionary<T>(Dictionary<long, T> result) where T : IMessage, new()
        {
            var map = GetValue(LongKeyMap.Parser);
            map?.ToDictionary(result);
        }

        /// <summary>
        /// 获取 string 键字典，填充到指定结果字典中（复用传入的字典容器）。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void GetDictionary<T>(Dictionary<string, T> result) where T : IMessage, new()
        {
            var map = GetValue(StringKeyMap.Parser);
            map?.ToDictionary(result);
        }

        #endregion
    }
}
