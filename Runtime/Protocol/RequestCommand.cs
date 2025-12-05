using System.Collections.Generic;
using T2FGame.Client.Utilities;
using T2FGame.Protocol;
using Google.Protobuf;

namespace T2FGame.Client.Protocol
{
    
    /// <summary>
    /// 表示一个网络请求命令，用于封装客户端向服务器发送的消息。
    /// </summary>
    public sealed class RequestCommand
    {
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

        public void Initialize(int cmdMerge,ByteString data, CommandType commandType = CommandType.Business)
        {
            MsgId = commandType == CommandType.Heartbeat ? 0 : MsgIdManager.GenerateNextMsgId();
            CmdMerge = cmdMerge;
            Data = data ?? ByteString.Empty;
            CommandType = commandType;
        }
        
        /// <summary>
        /// 将当前请求命令封装成字节数组以便在网络中传输。
        /// 使用 ExternalMessage 对象作为中间结构体完成序列化操作。
        /// </summary>
        /// <returns>表示整个请求命令的字节数组。</returns>
        public byte[] Encapsulate()
        {
            var externalMsg = new ExternalMessage
            {
                CmdCode = (int)CommandType,
                CmdMerge = CmdMerge,
                MsgId = MsgId,
                Data = Data,
            };
            return externalMsg.ToByteArray();
        }
        
        /// <summary>
        /// 重置当前请求命令的所有属性到初始状态。
        /// </summary>
        public void Reset()
        {
            MsgId = 0;
            CmdMerge = 0;
            Data = ByteString.Empty;
            CommandType = CommandType.Business;
        }

        #region  xxxxxx
        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识。
        /// 数据部分为空，命令类型默认为业务类型。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand Of(int cmdMerge)
        {
            return Of(cmdMerge, ByteString.Empty);
        }

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识及 Protobuf 消息数据。
        /// 若传入的消息为空，则数据部分将被设为空字符串。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="message">要封装进请求中的 Protobuf 消息对象。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand Of(int cmdMerge, IMessage message)
        {
            var byteString = message?.ToByteString() ?? ByteString.Empty;
            return Of(cmdMerge, byteString);
        }

        /// <summary>
        /// 创建一个新的请求命令实例，并指定其业务路由标识、数据内容以及命令类型。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">请求携带的数据内容。</param>
        /// <param name="commandType">请求命令类型，默认为业务类型。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand Of(int cmdMerge, ByteString data, CommandType commandType = CommandType.Business)
        {
            var requestCommand = ReferencePool<RequestCommand>.Spawn();
            requestCommand.Initialize(cmdMerge, data, commandType);
            return requestCommand;
        }

        /// <summary>
        /// 创建一个专门用于心跳检测的请求命令实例。
        /// 路由标识为 0，数据为空，命令类型为心跳。
        /// </summary>
        /// <returns>新创建的心跳请求命令实例。</returns>
        public static RequestCommand Heartbeat()
        {
            return Of(0, ByteString.Empty, CommandType.Heartbeat);
        }

        /// <summary>
        /// 创建一个包含整型数值的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的整型数值。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfInt(int cmdMerge, int data)
        {
            return Of(cmdMerge, new IntValue { Value = data });
        }

        /// <summary>
        /// 创建一个包含整型列表的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的整型列表。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfIntList(int cmdMerge, List<int> data)
        {
            var message = new IntValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含整型集合的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的整型集合。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfIntList(int cmdMerge, IEnumerable<int> data)
        {
            var message = new IntValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含布尔值的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的布尔值。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfBool(int cmdMerge, bool data)
        {
            return Of(cmdMerge, new BoolValue { Value = data });
        }

        /// <summary>
        /// 创建一个包含布尔值列表的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的布尔值列表。</param>
        /// <returns>新创建的请求命令实例。</returns>
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
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的字符串值。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfString(int cmdMerge, string data)
        {
            return Of(cmdMerge, new StringValue { Value = data ?? string.Empty });
        }

        /// <summary>
        /// 创建一个包含字符串列表的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的字符串列表。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfStringList(int cmdMerge, List<string> data)
        {
            var message = new StringValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含长整型数值的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的长整型数值。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfLong(int cmdMerge, long data)
        {
            return Of(cmdMerge, new LongValue { Value = data });
        }

        /// <summary>
        /// 创建一个包含长整型列表的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的长整型列表。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfLongList(int cmdMerge, List<long> data)
        {
            var message = new LongValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }
        

        /// <summary>
        /// 创建一个包含整型数组的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的整型数组。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfIntArray(int cmdMerge, int[] data)
        {
            var message = new IntValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }

        /// <summary>
        /// 创建一个包含字符串数组的请求命令实例。
        /// </summary>
        /// <param name="cmdMerge">业务路由标识。</param>
        /// <param name="data">需要传递的字符串数组。</param>
        /// <returns>新创建的请求命令实例。</returns>
        public static RequestCommand OfStringArray(int cmdMerge, string[] data)
        {
            var message = new StringValueList();
            if (data != null)
                message.Values.AddRange(data);
            return Of(cmdMerge, message);
        }
        #endregion
    }
}