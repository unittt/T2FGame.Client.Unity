using System.Collections.Generic;
using Google.Protobuf;

namespace Pisces.Protocol
{
    #region 1. 基础类型扩展 (IntValue, LongValue, etc.)

    /// <summary>
    /// IntValue 消息类型的扩展类，提供与基础类型 int 的隐式转换
    /// </summary>
    public partial class IntValue 
    {
        /// <summary>
        /// 将 int 类型隐式转换为 IntValue 包装器
        /// </summary>
        /// <param name="value">要包装的 int 值</param>
        /// <returns>包含指定值的 IntValue 对象</returns>
        public static implicit operator IntValue(int value) => new() { Value = value };
        
        /// <summary>
        /// 将 IntValue 包装器隐式转换为 int 类型
        /// </summary>
        /// <param name="wrapper">IntValue 包装器对象</param>
        /// <returns>包装的 int 值，如果包装器为 null 则返回 0</returns>
        public static implicit operator int(IntValue wrapper) => wrapper?.Value ?? 0;
    }

    /// <summary>
    /// LongValue 消息类型的扩展类，提供与基础类型 long 的隐式转换
    /// </summary>
    public partial class LongValue 
    {
        /// <summary>
        /// 将 long 类型隐式转换为 LongValue 包装器
        /// </summary>
        /// <param name="value">要包装的 long 值</param>
        /// <returns>包含指定值的 LongValue 对象</returns>
        public static implicit operator LongValue(long value) => new() { Value = value };
        
        /// <summary>
        /// 将 LongValue 包装器隐式转换为 long 类型
        /// </summary>
        /// <param name="wrapper">LongValue 包装器对象</param>
        /// <returns>包装的 long 值，如果包装器为 null 则返回 0L</returns>
        public static implicit operator long(LongValue wrapper) => wrapper?.Value ?? 0L;
    }

    /// <summary>
    /// StringValue 消息类型的扩展类，提供与基础类型 string 的隐式转换
    /// </summary>
    public partial class StringValue
    {
        /// <summary>
        /// 将 string 类型隐式转换为 StringValue 包装器
        /// </summary>
        /// <param name="value">要包装的字符串值</param>
        /// <returns>包含指定值的 StringValue 对象</returns>
        public static implicit operator StringValue(string value) => new() { Value = value ?? string.Empty };
        
        /// <summary>
        /// 将 StringValue 包装器隐式转换为 string 类型
        /// </summary>
        /// <param name="wrapper">StringValue 包装器对象</param>
        /// <returns>包装的字符串值，如果包装器为 null 则返回空字符串</returns>
        public static implicit operator string(StringValue wrapper) => wrapper?.Value ?? string.Empty;
    }

    /// <summary>
    /// BoolValue 消息类型的扩展类，提供与基础类型 bool 的隐式转换
    /// </summary>
    public partial class BoolValue 
    {
        /// <summary>
        /// 将 bool 类型隐式转换为 BoolValue 包装器
        /// </summary>
        /// <param name="value">要包装的布尔值</param>
        /// <returns>包含指定值的 BoolValue 对象</returns>
        public static implicit operator BoolValue(bool value) => new() { Value = value };
        
        /// <summary>
        /// 将 BoolValue 包装器隐式转换为 bool 类型
        /// </summary>
        /// <param name="wrapper">BoolValue 包装器对象</param>
        /// <returns>包装的布尔值，如果包装器为 null 则返回 false</returns>
        public static implicit operator bool(BoolValue wrapper) => wrapper?.Value ?? false;
    }

    #endregion

    #region 2. 基础列表扩展 (IntValueList, LongValueList, etc.)

    /// <summary>
    /// IntValueList 消息类型的扩展类，提供与 List&lt;int&gt; 和 int[] 数组的隐式转换
    /// </summary>
    public partial class IntValueList 
    {
        /// <summary>
        /// 将 List&lt;int&gt; 隐式转换为 IntValueList
        /// </summary>
        /// <param name="values">要转换的整数列表</param>
        /// <returns>包含指定值的 IntValueList 对象</returns>
        public static implicit operator IntValueList(List<int> values) => Create(values);
        
        /// <summary>
        /// 将 int[] 数组隐式转换为 IntValueList
        /// </summary>
        /// <param name="values">要转换的整数数组</param>
        /// <returns>包含指定值的 IntValueList 对象</returns>
        public static implicit operator IntValueList(int[] values) => Create(values);
        
        /// <summary>
        /// 创建 IntValueList 对象并填充指定的值
        /// </summary>
        /// <param name="v">要填充的整数值集合</param>
        /// <returns>包含指定值的 IntValueList 对象</returns>
        private static IntValueList Create(IList<int> v) {
            var l = new IntValueList();
            if (v != null) l.Values.AddRange(v);
            return l;
        }
        
        /// <summary>
        /// 将当前 IntValueList 中的值填充到指定的结果列表中
        /// </summary>
        /// <param name="result">用于接收结果的列表</param>
        public void ToList(List<int> result) => CollectionConvertHelper.FillList(Values, result, i => i);
    }

    /// <summary>
    /// LongValueList 消息类型的扩展类，提供与 List&lt;long&gt; 和 long[] 数组的隐式转换
    /// </summary>
    public partial class LongValueList 
    {
        /// <summary>
        /// 将 List&lt;long&gt; 隐式转换为 LongValueList
        /// </summary>
        /// <param name="values">要转换的长整数列表</param>
        /// <returns>包含指定值的 LongValueList 对象</returns>
        public static implicit operator LongValueList(List<long> values) => Create(values);
        
        /// <summary>
        /// 将 long[] 数组隐式转换为 LongValueList
        /// </summary>
        /// <param name="values">要转换的长整数数组</param>
        /// <returns>包含指定值的 LongValueList 对象</returns>
        public static implicit operator LongValueList(long[] values) => Create(values);
        
        /// <summary>
        /// 创建 LongValueList 对象并填充指定的值
        /// </summary>
        /// <param name="v">要填充的长整数值集合</param>
        /// <returns>包含指定值的 LongValueList 对象</returns>
        private static LongValueList Create(IList<long> v)
        {
            var l = new LongValueList();
            if (v != null) l.Values.AddRange(v);
            return l;
        }
        
        /// <summary>
        /// 将当前 LongValueList 中的值填充到指定的结果列表中
        /// </summary>
        /// <param name="result">用于接收结果的列表</param>
        public void ToList(List<long> result) => CollectionConvertHelper.FillList(Values, result, i => i);
    }

    /// <summary>
    /// StringValueList 消息类型的扩展类，提供与 List&lt;string&gt; 和 string[] 数组的隐式转换
    /// </summary>
    public partial class StringValueList 
    {
        /// <summary>
        /// 将 List&lt;string&gt; 隐式转换为 StringValueList
        /// </summary>
        /// <param name="values">要转换的字符串列表</param>
        /// <returns>包含指定值的 StringValueList 对象</returns>
        public static implicit operator StringValueList(List<string> values) => Create(values);
        
        /// <summary>
        /// 将 string[] 数组隐式转换为 StringValueList
        /// </summary>
        /// <param name="values">要转换的字符串数组</param>
        /// <returns>包含指定值的 StringValueList 对象</returns>
        public static implicit operator StringValueList(string[] values) => Create(values);
        
        /// <summary>
        /// 创建 StringValueList 对象并填充指定的值
        /// </summary>
        /// <param name="v">要填充的字符串值集合</param>
        /// <returns>包含指定值的 StringValueList 对象</returns>
        private static StringValueList Create(IList<string> v) {
            var l = new StringValueList();
            if (v != null) l.Values.AddRange(v);
            return l;
        }
        
        /// <summary>
        /// 将当前 StringValueList 中的值填充到指定的结果列表中
        /// </summary>
        /// <param name="result">用于接收结果的列表</param>
        public void ToList(List<string> result) => CollectionConvertHelper.FillList(Values, result, i => i);
    }

    /// <summary>
    /// BoolValueList 消息类型的扩展类，提供与 List&lt;bool&gt; 和 bool[] 数组的隐式转换
    /// </summary>
    public partial class BoolValueList 
    {
        /// <summary>
        /// 将 List&lt;bool&gt; 隐式转换为 BoolValueList
        /// </summary>
        /// <param name="values">要转换的布尔列表</param>
        /// <returns>包含指定值的 BoolValueList 对象</returns>
        public static implicit operator BoolValueList(List<bool> values) => Create(values);
        
        /// <summary>
        /// 将 bool[] 数组隐式转换为 BoolValueList
        /// </summary>
        /// <param name="values">要转换的布尔数组</param>
        /// <returns>包含指定值的 BoolValueList 对象</returns>
        public static implicit operator BoolValueList(bool[] values) => Create(values);
        
        /// <summary>
        /// 创建 BoolValueList 对象并填充指定的值
        /// </summary>
        /// <param name="v">要填充的布尔值集合</param>
        /// <returns>包含指定值的 BoolValueList 对象</returns>
        private static BoolValueList Create(IList<bool> v) {
            var l = new BoolValueList();
            if (v != null) l.Values.AddRange(v);
            return l;
        }
        
        /// <summary>
        /// 将当前 BoolValueList 中的值填充到指定的结果列表中
        /// </summary>
        /// <param name="result">用于接收结果的列表</param>
        public void ToList(List<bool> result) => CollectionConvertHelper.FillList(Values, result, i => i);
    }

    #endregion

    #region 3. ByteValueList (Protobuf 消息列表)

    /// <summary>
    /// ByteValueList 消息类型的扩展类，用于处理 Protobuf 消息的字节序列化列表
    /// </summary>
    public partial class ByteValueList
    {
        /// <summary>
        /// 从 Protobuf 消息列表创建 ByteValueList
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="messages">要序列化的消息列表</param>
        /// <returns>包含消息字节表示的 ByteValueList 对象</returns>
        public static ByteValueList From<T>(List<T> messages) where T : IMessage<T> 
        {
            var list = new ByteValueList();
            CollectionConvertHelper.FillRepeatedField(messages, list.Values, m => m.ToByteString());
            return list;
        }

        // No-GC 反序列化版本
        /// <summary>
        /// 将当前 ByteValueList 中的字节数据反序列化为指定类型的消息列表
        /// </summary>
        /// <typeparam name="T">实现 IMessage 接口且具有无参构造函数的消息类型</typeparam>
        /// <param name="result">用于接收反序列化结果的列表</param>
        public void ToList<T>(List<T> result) where T : IMessage, new() => CollectionConvertHelper.FillList(Values, result, ProtoSerializer.Deserialize<T>, true);

        /// <summary>
        /// 使用指定的消息解析器将当前 ByteValueList 中的字节数据反序列化为指定类型的消息列表
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="result">用于接收反序列化结果的列表</param>
        /// <param name="parser">用于反序列化消息的解析器</param>
        public void ToList<T>(List<T> result, MessageParser<T> parser) where T : IMessage<T> => 
            CollectionConvertHelper.FillList(Values, result, bs => ProtoSerializer.Deserialize(bs, parser), true);

        /// <summary>
        /// 向当前 ByteValueList 添加一个 Protobuf 消息
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="message">要添加的消息对象</param>
        public void Add<T>(T message) where T : IMessage<T> 
        {
            if (message != null) Values.Add(message.ToByteString());
        }
    }

    #endregion

    #region 4. 字典映射扩展 (Map Extensions)

    /// <summary>
    /// IntKeyMap 消息类型的扩展类，提供字典操作功能
    /// </summary>
    public partial class IntKeyMap
    {
        /// <summary>
        /// 从字典创建 IntKeyMap
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="dict">源字典</param>
        /// <returns>包含字典数据的 IntKeyMap 对象</returns>
        public static IntKeyMap From<T>(Dictionary<int, T> dict) where T : IMessage<T>
        {
            var map = new IntKeyMap();
            CollectionConvertHelper.FillEntriesFromDictionary(dict, map.Entries, 
                (k, v) => new IntKeyEntry { Key = k, Value = v.ToByteString() });
            return map;
        }

        /// <summary>
        /// 将当前 IntKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage 接口且具有无参构造函数的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void ToDictionary<T>(Dictionary<int, T> result) where T : IMessage, new() =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize<T>(e.Value));

        /// <summary>
        /// 使用指定的消息解析器将当前 IntKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        /// <param name="parser">用于反序列化消息的解析器</param>
        public void ToDictionary<T>(Dictionary<int, T> result, MessageParser<T> parser) where T : IMessage<T> =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize(e.Value, parser));
    }

    /// <summary>
    /// LongKeyMap 消息类型的扩展类，提供字典操作功能
    /// </summary>
    public partial class LongKeyMap 
    {
        /// <summary>
        /// 从字典创建 LongKeyMap
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="dict">源字典</param>
        /// <returns>包含字典数据的 LongKeyMap 对象</returns>
        public static LongKeyMap From<T>(Dictionary<long, T> dict) where T : IMessage<T>
        {
            var map = new LongKeyMap();
            CollectionConvertHelper.FillEntriesFromDictionary(dict, map.Entries, 
                (k, v) => new LongKeyEntry { Key = k, Value = v.ToByteString() });
            return map;
        }

        /// <summary>
        /// 将当前 LongKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage 接口且具有无参构造函数的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void ToDictionary<T>(Dictionary<long, T> result) where T : IMessage, new() =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize<T>(e.Value));

        /// <summary>
        /// 使用指定的消息解析器将当前 LongKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        /// <param name="parser">用于反序列化消息的解析器</param>
        public void ToDictionary<T>(Dictionary<long, T> result, MessageParser<T> parser) where T : IMessage<T> =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize(e.Value, parser));
    }

    public partial class StringKeyMap
    {
        /// <summary>
        /// 从字典创建 StringKeyMap
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="dict">源字典</param>
        /// <returns>包含字典数据的 StringKeyMap 对象</returns>
        public static StringKeyMap From<T>(Dictionary<string, T> dict) where T : IMessage<T>
        {
            var map = new StringKeyMap();
            CollectionConvertHelper.FillEntriesFromDictionary(dict, map.Entries, 
                (k, v) => new StringKeyEntry { Key = k, Value = v.ToByteString() });
            return map;
        }

        /// <summary>
        /// 将当前 StringKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage 接口且具有无参构造函数的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void ToDictionary<T>(Dictionary<string, T> result) where T : IMessage, new() =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize<T>(e.Value));

        /// <summary>
        /// 使用指定的消息解析器将当前 StringKeyMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        /// <param name="parser">用于反序列化消息的解析器</param>
        public void ToDictionary<T>(Dictionary<string, T> result, MessageParser<T> parser) where T : IMessage<T> =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize(e.Value, parser));
    }

    /// <summary>
    /// ByteValueMap 消息类型的扩展类，提供字典操作功能
    /// </summary>
    public partial class ByteValueMap
    {
        /// <summary>
        /// 从字典创建 ByteValueMap
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="dict">源字典</param>
        /// <returns>包含字典数据的 ByteValueMap 对象</returns>
        public static ByteValueMap From<T>(Dictionary<ByteString, T> dict) where T : IMessage<T>
        {
            var map = new ByteValueMap();
            CollectionConvertHelper.FillEntriesFromDictionary(dict, map.Entries, 
                (k, v) => new ByteValueEntry { Key = k, Value = v.ToByteString() });
            return map;
        }

        /// <summary>
        /// 将当前 ByteValueMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage 接口且具有无参构造函数的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        public void ToDictionary<T>(Dictionary<ByteString, T> result) where T : IMessage, new() =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize<T>(e.Value));

        /// <summary>
        /// 使用指定的消息解析器将当前 ByteValueMap 中的条目填充到指定的字典中
        /// </summary>
        /// <typeparam name="T">实现 IMessage&lt;T&gt; 接口的消息类型</typeparam>
        /// <param name="result">用于接收结果的字典</param>
        /// <param name="parser">用于反序列化消息的解析器</param>
        public void ToDictionary<T>(Dictionary<ByteString, T> result, MessageParser<T> parser) where T : IMessage<T> =>
            CollectionConvertHelper.FillDictionary(Entries, result, e => e.Key, e => ProtoSerializer.Deserialize(e.Value, parser));
    }

    #endregion

    #region 5. Vector 扩展 (Unity 集成)

    /// <summary>
    /// Vector2 消息类型的扩展类，提供与 Unity Vector2 的隐式转换
    /// </summary>
    public partial class Vector2 
    {
        /// <summary>
        /// 将 Unity Vector2 隐式转换为 Vector2 消息类型
        /// </summary>
        /// <param name="v">要转换的 Unity Vector2 对象</param>
        /// <returns>包含相同坐标值的 Vector2 消息对象</returns>
        public static implicit operator Vector2(UnityEngine.Vector2 v) => new() { X = v.x, Y = v.y };
        
        /// <summary>
        /// 将 Vector2 消息类型隐式转换为 Unity Vector2
        /// </summary>
        /// <param name="v">Vector2 消息对象</param>
        /// <returns>包含相同坐标值的 Unity Vector2 对象，如果消息对象为 null 则返回 Vector2.zero</returns>
        public static implicit operator UnityEngine.Vector2(Vector2 v) => v != null ? new UnityEngine.Vector2(v.X, v.Y) : UnityEngine.Vector2.zero;
    }

    /// <summary>
    /// Vector3 消息类型的扩展类，提供与 Unity Vector3 的隐式转换
    /// </summary>
    public partial class Vector3 
    {
        /// <summary>
        /// 将 Unity Vector3 隐式转换为 Vector3 消息类型
        /// </summary>
        /// <param name="v">要转换的 Unity Vector3 对象</param>
        /// <returns>包含相同坐标值的 Vector3 消息对象</returns>
        public static implicit operator Vector3(UnityEngine.Vector3 v) => new() { X = v.x, Y = v.y, Z = v.z };
        
        /// <summary>
        /// 将 Vector3 消息类型隐式转换为 Unity Vector3
        /// </summary>
        /// <param name="v">Vector3 消息对象</param>
        /// <returns>包含相同坐标值的 Unity Vector3 对象，如果消息对象为 null 则返回 Vector3.zero</returns>
        public static implicit operator UnityEngine.Vector3(Vector3 v) => v != null ? new UnityEngine.Vector3(v.X, v.Y, v.Z) : UnityEngine.Vector3.zero;
    }

    /// <summary>
    /// Vector3List 消息类型的扩展类，提供与 Unity Vector3 列表的转换功能
    /// </summary>
    public partial class Vector3List 
    {
        // No-GC: RepeatedField<Vector3> -> List<UnityEngine.Vector3>
        /// <summary>
        /// 将当前 Vector3List 中的值填充到指定的 Unity Vector3 列表中
        /// </summary>
        /// <param name="result">用于接收结果的 Unity Vector3 列表</param>
        public void ToList(List<UnityEngine.Vector3> result) => CollectionConvertHelper.FillList(Values, result, v => (UnityEngine.Vector3)v);

        /// <summary>
        /// 将 Unity Vector3 列表隐式转换为 Vector3List
        /// </summary>
        /// <param name="values">要转换的 Unity Vector3 列表</param>
        /// <returns>包含指定值的 Vector3List 对象</returns>
        public static implicit operator Vector3List(List<UnityEngine.Vector3> values) 
        {
            var list = new Vector3List();
            CollectionConvertHelper.FillRepeatedField(values, list.Values, v => v);
            return list;
        }
    }

    /// <summary>
    /// Vector2Int 消息类型的扩展类，提供与 Unity Vector2Int 的隐式转换
    /// </summary>
    public partial class Vector2Int 
    {
        /// <summary>
        /// 将 Unity Vector2Int 隐式转换为 Vector2Int 消息类型
        /// </summary>
        /// <param name="v">要转换的 Unity Vector2Int 对象</param>
        /// <returns>包含相同坐标值的 Vector2Int 消息对象</returns>
        public static implicit operator Vector2Int(UnityEngine.Vector2Int v) => new() { X = v.x, Y = v.y };
        
        /// <summary>
        /// 将 Vector2Int 消息类型隐式转换为 Unity Vector2Int
        /// </summary>
        /// <param name="v">Vector2Int 消息对象</param>
        /// <returns>包含相同坐标值的 Unity Vector2Int 对象，如果消息对象为 null 则返回 Vector2Int.zero</returns>
        public static implicit operator UnityEngine.Vector2Int(Vector2Int v) => v != null ? new UnityEngine.Vector2Int(v.X, v.Y) : UnityEngine.Vector2Int.zero;
    }

    /// <summary>
    /// Vector3Int 消息类型的扩展类，提供与 Unity Vector3Int 的隐式转换
    /// </summary>
    public partial class Vector3Int 
    {
        /// <summary>
        /// 将 Unity Vector3Int 隐式转换为 Vector3Int 消息类型
        /// </summary>
        /// <param name="v">要转换的 Unity Vector3Int 对象</param>
        /// <returns>包含相同坐标值的 Vector3Int 消息对象</returns>
        public static implicit operator Vector3Int(UnityEngine.Vector3Int v) => new() { X = v.x, Y = v.y, Z = v.z };
        
        /// <summary>
        /// 将 Vector3Int 消息类型隐式转换为 Unity Vector3Int
        /// </summary>
        /// <param name="v">Vector3Int 消息对象</param>
        /// <returns>包含相同坐标值的 Unity Vector3Int 对象，如果消息对象为 null 则返回 Vector3Int.zero</returns>
        public static implicit operator UnityEngine.Vector3Int(Vector3Int v) => v != null ? new UnityEngine.Vector3Int(v.X, v.Y, v.Z) : UnityEngine.Vector3Int.zero;
    }

    /// <summary>
    /// Vector2List 消息类型的扩展类，提供与 Unity Vector2 列表的转换功能
    /// </summary>
    public partial class Vector2List 
    {
        /// <summary>
        /// 将当前 Vector2List 中的值填充到指定的 Unity Vector2 列表中
        /// </summary>
        /// <param name="result">用于接收结果的 Unity Vector2 列表</param>
        public void ToList(List<UnityEngine.Vector2> result) => CollectionConvertHelper.FillList(Values, result, v => (UnityEngine.Vector2)v);

        /// <summary>
        /// 将 Unity Vector2 列表隐式转换为 Vector2List
        /// </summary>
        /// <param name="values">要转换的 Unity Vector2 列表</param>
        /// <returns>包含指定值的 Vector2List 对象</returns>
        public static implicit operator Vector2List(List<UnityEngine.Vector2> values) 
        {
            var list = new Vector2List();
            CollectionConvertHelper.FillRepeatedField(values, list.Values, v => v);
            return list;
        }
    }

    /// <summary>
    /// Vector2IntList 消息类型的扩展类，提供与 Unity Vector2Int 列表的转换功能
    /// </summary>
    public partial class Vector2IntList 
    {
        /// <summary>
        /// 将当前 Vector2IntList 中的值填充到指定的 Unity Vector2Int 列表中
        /// </summary>
        /// <param name="result">用于接收结果的 Unity Vector2Int 列表</param>
        public void ToList(List<UnityEngine.Vector2Int> result) => CollectionConvertHelper.FillList(Values, result, v => (UnityEngine.Vector2Int)v);

        /// <summary>
        /// 将 Unity Vector2Int 列表隐式转换为 Vector2IntList
        /// </summary>
        /// <param name="values">要转换的 Unity Vector2Int 列表</param>
        /// <returns>包含指定值的 Vector2IntList 对象</returns>
        public static implicit operator Vector2IntList(List<UnityEngine.Vector2Int> values) 
        {
            var list = new Vector2IntList();
            CollectionConvertHelper.FillRepeatedField(values, list.Values, v => v);
            return list;
        }
    }

    /// <summary>
    /// Vector3IntList 消息类型的扩展类，提供与 Unity Vector3Int 列表的转换功能
    /// </summary>
    public partial class Vector3IntList 
    {
        /// <summary>
        /// 将当前 Vector3IntList 中的值填充到指定的 Unity Vector3Int 列表中
        /// </summary>
        /// <param name="result">用于接收结果的 Unity Vector3Int 列表</param>
        public void ToList(List<UnityEngine.Vector3Int> result) => CollectionConvertHelper.FillList(Values, result, v => (UnityEngine.Vector3Int)v);

        /// <summary>
        /// 将 Unity Vector3Int 列表隐式转换为 Vector3IntList
        /// </summary>
        /// <param name="values">要转换的 Unity Vector3Int 列表</param>
        /// <returns>包含指定值的 Vector3IntList 对象</returns>
        public static implicit operator Vector3IntList(List<UnityEngine.Vector3Int> values) 
        {
            var list = new Vector3IntList();
            CollectionConvertHelper.FillRepeatedField(values, list.Values, v => v);
            return list;
        }
    }

    #endregion
}
