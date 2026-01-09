using Google.Protobuf;
using Pisces.Client.Utils;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 表示一个网络请求命令，用于封装客户端向服务器发送的消息。
    /// </summary>
    public sealed partial class RequestCommand : IPoolable
    {
        private static readonly ByteString _emptyByteString = ByteString.Empty;

        /// <summary>
        /// 获取消息标记号。该字段由前端在发起请求时设置，
        /// 服务端在响应时会原样带回，用于匹配请求与响应。
        /// </summary>
        public int MsgId { get; private set; }

        /// <summary>
        /// 获取业务路由标识。采用合并编码方式：高16位表示主命令，优16位表示子命令。
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

        internal void Initialize(int cmdMerge, ByteString data, CommandType commandType = CommandType.Business)
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
        public void OnSpawn() { }

        /// <summary>
        /// IPoolable: 归还到池中时调用
        /// </summary>
        public void OnDespawn() => Reset();
    }
}
