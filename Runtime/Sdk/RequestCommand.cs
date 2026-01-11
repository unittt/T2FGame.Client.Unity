using Google.Protobuf;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 表示一个网络请求命令，用于封装客户端向服务器发送的消息。
    /// </summary>
    public sealed partial class RequestCommand : IPoolable
    {
        private static readonly ByteString _emptyByteString = ByteString.Empty;

        public MessageType MessageType { get; private set; }

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

        private void Initialize(int cmdMerge, ByteString data, MessageType messageType = MessageType.Business)
        {
            CmdMerge = cmdMerge;
            Data = data ?? _emptyByteString;
            MessageType = messageType;
            // 生成消息编号 (只有业务类型 才会有消息编号)
            MsgId = messageType == MessageType.Business ? MsgIdManager.GenerateNextMsgId() : 0;
        }

        /// <summary>
        /// 重置当前请求命令的所有属性到初始状态。
        /// </summary>
        public void Reset()
        {
            MsgId = 0;
            CmdMerge = 0;
            Data = _emptyByteString;
            MessageType = MessageType.Heartbeat;
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
