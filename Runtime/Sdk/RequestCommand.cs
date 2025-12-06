using System.Collections.Generic;
using Google.Protobuf;
using T2FGame.Client.Utils;
using T2FGame.Protocol;

namespace T2FGame.Client.Sdk
{
    /// <summary>
    /// 表示一个网络请求命令，用于封装客户端向服务器发送的消息。
    /// </summary>
    public sealed class RequestCommand : IPoolable
    {
        private static readonly ByteString _emptyByteString = ByteString.Empty;

        /// <summary>
        /// 获取消息标记号。该字段由前端在发起请求时设置，
        /// 服务端在响应时会原样带回，用于匹配请求与响应。
        /// </summary>
        public int MsgId { get; private set; }

        /// <summary>
        /// 获取业务路由标识。采用合并编码方式：高16位表示主命令，低16位表示子命令。
        /// </summary>
        public int CmdMerge { get; private set; }

        /// <summary>
        /// 获取请求数据内容
        /// </summary>
        public ByteString Data { get; private set; }

        /// <summary>
        /// 获取请求命令类型，默认为业务类型（Business）。
        /// 可选值包括心跳（Heartbeat）和业务（Business）两种类型。
        /// </summary>
        public CommandType CommandType { get; private set; } = CommandType.Business;

        public void Initialize(
            int cmdMerge,
            ByteString data,
            CommandType commandType = CommandType.Business
        )
        {
            CmdMerge = cmdMerge;
            Data = data ?? _emptyByteString;
            CommandType = commandType;
            MsgId = commandType == CommandType.Heartbeat ? 0 : MsgIdManager.GenerateNextMsgId();
        }

        /// <summary>
        /// 重置当前请求命令的所有属性到初始状态。
        /// </summary>
        public void Reset()
        {
            MsgId = 0;
            CmdMerge = 0;
            Data = _emptyByteString;
            CommandType = CommandType.Business;
        }

        /// <summary>
        /// IPoolable: 从池中取出时调用
        /// </summary>
        public void OnSpawn() { } // 无需操作，Initialize 会设置所有属性

        /// <summary>
        /// IPoolable: 归还到池中时调用
        /// </summary>
        public void OnDespawn() => Reset();

        #region 工厂方法

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识、数据内容以及命令类型。
        /// </summary>
        public static RequestCommand Of(
            int cmdMerge,
            ByteString data,
            CommandType commandType = CommandType.Business
        )
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
        public static RequestCommand Of(int cmdMerge, IMessage message)
        {
            var byteString = message?.ToByteString() ?? _emptyByteString;
            return Of(cmdMerge, byteString);
        }

        /// <summary>
        /// 创建一个专门用于心跳检测的请求命令实例。
        /// 路由标识为 0，数据为空，命令类型为心跳。
        /// </summary>
        public static RequestCommand Heartbeat() => Of(0, _emptyByteString, CommandType.Heartbeat);

        /// <summary>
        /// 创建一个包含整型数值的请求命令实例。
        /// </summary>
        public static RequestCommand OfInt(int cmdMerge, int data) =>
            Of(cmdMerge, new IntValue { Value = data });

        /// <summary>
        /// 创建一个包含整型集合的请求命令实例。
        /// </summary>
        public static RequestCommand OfIntList(int cmdMerge, IEnumerable<int> data)
        {
            var message = new IntValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含整型数组的请求命令实例。
        /// </summary>
        public static RequestCommand OfIntArray(int cmdMerge, int[] data) =>
            OfIntList(cmdMerge, data);

        /// <summary>
        /// 创建一个包含布尔值的请求命令实例。
        /// </summary>
        public static RequestCommand OfBool(int cmdMerge, bool data) =>
            Of(cmdMerge, new BoolValue { Value = data });

        /// <summary>
        /// 创建一个包含布尔值列表的请求命令实例。
        /// </summary>
        public static RequestCommand OfBoolList(int cmdMerge, List<bool> data)
        {
            var message = new BoolValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含字符串值的请求命令实例。
        /// </summary>
        public static RequestCommand OfString(int cmdMerge, string data) =>
            Of(cmdMerge, new StringValue { Value = data ?? string.Empty });

        /// <summary>
        /// 创建一个包含字符串集合的请求命令实例。
        /// </summary>
        public static RequestCommand OfStringList(int cmdMerge, IEnumerable<string> data)
        {
            var message = new StringValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含字符串数组的请求命令实例。
        /// </summary>
        public static RequestCommand OfStringArray(int cmdMerge, string[] data) =>
            OfStringList(cmdMerge, data);

        /// <summary>
        /// 创建一个包含长整型数值的请求命令实例。
        /// </summary>
        public static RequestCommand OfLong(int cmdMerge, long data) =>
            Of(cmdMerge, new LongValue { Value = data });

        /// <summary>
        /// 创建一个包含长整型集合的请求命令实例。
        /// </summary>
        public static RequestCommand OfLongList(int cmdMerge, IEnumerable<long> data)
        {
            var message = new LongValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }
        #endregion
    }
}
