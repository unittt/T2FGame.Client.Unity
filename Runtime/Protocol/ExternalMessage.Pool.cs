using Google.Protobuf;
using Pisces.Client.Utils;

namespace Pisces.Protocol
{
    /// <summary>
    /// ExternalMessage 对象池扩展
    /// 实现 IPoolable 接口以支持对象复用
    /// </summary>
    public sealed partial class ExternalMessage : IPoolable
    {
        /// <summary>
        /// 从池中取出时调用
        /// </summary>
        public void OnSpawn()
        {
            // Protobuf 对象默认值已经是正确的初始状态
        }

        /// <summary>
        /// 归还到池中时调用，清理所有字段
        /// </summary>
        public void OnDespawn()
        {
            messageType_ = MessageType.Heartbeat;
            protocolSwitch_ = 0;
            cmdMerge_ = 0;
            responseStatus_ = 0;
            validMsg_ = "";
            data_ = ByteString.Empty;
            msgId_ = 0;
            _unknownFields = null;
        }
    }
}
