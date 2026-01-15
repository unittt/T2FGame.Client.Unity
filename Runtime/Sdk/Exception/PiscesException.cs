using System;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    //// <summary>
    ///  Pisces 框架异常基类
    /// </summary>
    public class PiscesException : Exception
    {
        /// <summary>
        /// 命令标识（如果有操作上下文）
        /// </summary>
        public CmdInfo? CmdInfo { get; }

        /// <summary>
        /// 消息ID（如果有操作上下文）
        /// </summary>
        public int MsgId { get; }

        /// <summary>
        /// 是否包含操作上下文（命令和消息ID）
        /// </summary>
        public bool HasOperationContext => CmdInfo.HasValue;

        protected PiscesException(string message, CmdInfo? cmdInfo = null, int msgId = 0, Exception inner = null) : base(message, inner)
        {
            CmdInfo = cmdInfo;
            MsgId = msgId;
        }

        /// <summary>
        /// 格式化上下文信息：(Cmd: Login, MsgId: 101)
        /// </summary>
        protected string GetContextSuffix()
        {
            if (!HasOperationContext) return string.Empty;
            return $" (Cmd: {CmdInfo}, MsgId: {MsgId})";
        }
    }
}
