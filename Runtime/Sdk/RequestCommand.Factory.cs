using System.Collections.Generic;
using Google.Protobuf;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// RequestCommand 工厂方法扩展
    /// </summary>
    public sealed partial class RequestCommand
    {
        #region 核心工厂方法

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识、数据内容以及命令类型。
        /// </summary>
        private static RequestCommand Of(int cmdMerge, ByteString data, MessageType commandType = MessageType.Business)
        {
            var requestCommand = ReferencePool<RequestCommand>.Spawn();
            requestCommand.Initialize(cmdMerge, data, commandType);
            return requestCommand;
        }

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识。
        /// 数据部分为空，命令类型默认为业务类型。
        /// </summary>
        public static RequestCommand Of(int cmdMerge) => Of(cmdMerge, _emptyByteString);

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识及 Protobuf 消息数据。
        /// 若传入的消息为空，则数据部分将被设为空字符串。
        /// </summary>
        public static RequestCommand Of<T>(int cmdMerge, T message) where T : IMessage
        {
            var byteString = message?.ToByteString() ?? _emptyByteString;
            return Of(cmdMerge, byteString);
        }

        /// <summary>
        /// 创建一个专门用于心跳检测的请求命令实例。
        /// 路由标识为 0，数据为空，命令类型为心跳。
        /// </summary>
        public static RequestCommand Heartbeat() => Of(0, _emptyByteString, MessageType.Heartbeat);

        /// <summary>
        /// 创建一个用于时间同步的请求命令实例。
        /// 路由标识为 0，包含客户端当前时间戳，命令类型为时间同步。
        /// </summary>
        public static RequestCommand TimeSync()
        {
            var msg = new TimeSyncMessage { ClientTime = TimeUtils.GetLocalTimeMs() };
            return Of(0, msg.ToByteString(), MessageType.TimeSync);
        }
        #endregion

        #region 基础类型重载（利用隐式转换）

        /// <summary>
        /// 创建一个包含整型数值的请求命令实例。
        /// 利用隐式转换 int → IntValue，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, 100);</example>
        public static RequestCommand Of(int cmdMerge, int data) => Of(cmdMerge, (IntValue)data);

        /// <summary>
        /// 创建一个包含字符串值的请求命令实例。
        /// 利用隐式转换 string → StringValue，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, "hello");</example>
        public static RequestCommand Of(int cmdMerge, string data) =>
            Of(cmdMerge, (StringValue)data);

        /// <summary>
        /// 创建一个包含长整型数值的请求命令实例。
        /// 利用隐式转换 long → LongValue，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, 999L);</example>
        public static RequestCommand Of(int cmdMerge, long data) => Of(cmdMerge, (LongValue)data);

        /// <summary>
        /// 创建一个包含布尔值的请求命令实例。
        /// 利用隐式转换 bool → BoolValue，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, true);</example>
        public static RequestCommand Of(int cmdMerge, bool data) => Of(cmdMerge, (BoolValue)data);

        /// <summary>
        /// 创建一个包含 Vector2 的请求命令实例。
        /// 利用隐式转换 UnityEngine.Vector2 → Vector2，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, new Vector2(1, 2));</example>
        public static RequestCommand Of(int cmdMerge, UnityEngine.Vector2 data) =>
            Of(cmdMerge, (Vector2)data);

        /// <summary>
        /// 创建一个包含 Vector3 的请求命令实例。
        /// 利用隐式转换 UnityEngine.Vector3 → Vector3，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, transform.position);</example>
        public static RequestCommand Of(int cmdMerge, UnityEngine.Vector3 data) =>
            Of(cmdMerge, (Vector3)data);

        /// <summary>
        /// 创建一个包含 Vector2Int 的请求命令实例。
        /// 利用隐式转换 UnityEngine.Vector2Int → Vector2Int，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, new Vector2Int(1, 2));</example>
        public static RequestCommand Of(int cmdMerge, UnityEngine.Vector2Int data) =>
            Of(cmdMerge, (Vector2Int)data);

        /// <summary>
        /// 创建一个包含 Vector3Int 的请求命令实例。
        /// 利用隐式转换 UnityEngine.Vector3Int → Vector3Int，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, new Vector3Int(1, 2, 3));</example>
        public static RequestCommand Of(int cmdMerge, UnityEngine.Vector3Int data) =>
            Of(cmdMerge, (Vector3Int)data);

        #endregion

        #region 列表类型重载（利用隐式转换）

        /// <summary>
        /// 创建一个包含整型列表的请求命令实例。
        /// 利用隐式转换 int[] → IntValueList，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, new int[] { 1, 2, 3 });</example>
        public static RequestCommand Of(int cmdMerge, int[] data) =>
            Of(cmdMerge, (IntValueList)data);

        /// <summary>
        /// 创建一个包含整型列表的请求命令实例。
        /// 利用隐式转换 List&lt;int&gt; → IntValueList，简化调用。
        /// </summary>
        /// <example>RequestCommand.Of(cmdMerge, myIntList);</example>
        public static RequestCommand Of(int cmdMerge, List<int> data) =>
            Of(cmdMerge, (IntValueList)data);

        /// <summary>
        /// 创建一个包含长整型列表的请求命令实例。
        /// 利用隐式转换 long[] → LongValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, long[] data) =>
            Of(cmdMerge, (LongValueList)data);

        /// <summary>
        /// 创建一个包含长整型列表的请求命令实例。
        /// 利用隐式转换 List&lt;long&gt; → LongValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<long> data) =>
            Of(cmdMerge, (LongValueList)data);

        /// <summary>
        /// 创建一个包含字符串列表的请求命令实例。
        /// 利用隐式转换 string[] → StringValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, string[] data) =>
            Of(cmdMerge, (StringValueList)data);

        /// <summary>
        /// 创建一个包含字符串列表的请求命令实例。
        /// 利用隐式转换 List&lt;string&gt; → StringValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<string> data) =>
            Of(cmdMerge, (StringValueList)data);

        /// <summary>
        /// 创建一个包含布尔列表的请求命令实例。
        /// 利用隐式转换 bool[] → BoolValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, bool[] data) =>
            Of(cmdMerge, (BoolValueList)data);

        /// <summary>
        /// 创建一个包含布尔列表的请求命令实例。
        /// 利用隐式转换 List&lt;bool&gt; → BoolValueList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<bool> data) =>
            Of(cmdMerge, (BoolValueList)data);

        /// <summary>
        /// 创建一个包含 Vector2 列表的请求命令实例。
        /// 利用隐式转换 List&lt;Vector2&gt; → Vector2List，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<UnityEngine.Vector2> data) =>
            Of(cmdMerge, (Vector2List)data);

        /// <summary>
        /// 创建一个包含 Vector3 列表的请求命令实例。
        /// 利用隐式转换 List&lt;Vector3&gt; → Vector3List，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<UnityEngine.Vector3> data) =>
            Of(cmdMerge, (Vector3List)data);

        /// <summary>
        /// 创建一个包含 Vector2Int 列表的请求命令实例。
        /// 利用隐式转换 List&lt;Vector2Int&gt; → Vector2IntList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<UnityEngine.Vector2Int> data) =>
            Of(cmdMerge, (Vector2IntList)data);

        /// <summary>
        /// 创建一个包含 Vector3Int 列表的请求命令实例。
        /// 利用隐式转换 List&lt;Vector3Int&gt; → Vector3IntList，简化调用。
        /// </summary>
        public static RequestCommand Of(int cmdMerge, List<UnityEngine.Vector3Int> data) =>
            Of(cmdMerge, (Vector3IntList)data);

        #endregion

        #region 泛型列表重载

        /// <summary>
        /// 创建一个包含 Protobuf 消息列表的请求命令实例。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="cmdMerge">业务路由标识</param>
        /// <param name="data">消息列表数据</param>
        /// <example>RequestCommand.Of(cmdMerge, myMessageList);</example>
        public static RequestCommand Of<T>(int cmdMerge, List<T> data)
            where T : IMessage<T> => Of(cmdMerge, ByteValueList.From(data));

        #endregion

        #region 字典类型重载

        /// <summary>
        /// 创建一个包含 int 键字典的请求命令实例。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="cmdMerge">业务路由标识</param>
        /// <param name="data">字典数据</param>
        /// <example>RequestCommand.Of(cmdMerge, myIntKeyDict);</example>
        public static RequestCommand Of<T>(int cmdMerge, Dictionary<int, T> data)
            where T : IMessage<T> => Of(cmdMerge, IntKeyMap.From(data));

        /// <summary>
        /// 创建一个包含 long 键字典的请求命令实例。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="cmdMerge">业务路由标识</param>
        /// <param name="data">字典数据</param>
        /// <example>RequestCommand.Of(cmdMerge, myLongKeyDict);</example>
        public static RequestCommand Of<T>(int cmdMerge, Dictionary<long, T> data)
            where T : IMessage<T> => Of(cmdMerge, LongKeyMap.From(data));

        /// <summary>
        /// 创建一个包含 string 键字典的请求命令实例。
        /// </summary>
        /// <typeparam name="T">Protobuf 消息类型</typeparam>
        /// <param name="cmdMerge">业务路由标识</param>
        /// <param name="data">字典数据</param>
        /// <example>RequestCommand.Of(cmdMerge, myStringKeyDict);</example>
        public static RequestCommand Of<T>(int cmdMerge, Dictionary<string, T> data)
            where T : IMessage<T> => Of(cmdMerge, StringKeyMap.From(data));

        #endregion
    }
}
