using System;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 客户端本地异常
    /// 用于处理 SDK 内部逻辑错误、网络连接状态、本地超时等
    /// </summary>
    public class PiscesClientException : PiscesException
    {
        /// <summary>
        /// 客户端本地错误码
        /// </summary>
        public PiscesClientCode ErrorCode { get; }

        /// <summary>
        /// 基础构造函数
        /// </summary>
        public PiscesClientException(PiscesClientCode errorCode, string message = null, Exception inner = null) : base(message ?? errorCode.GetMessage(), null, 0, inner)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 带请求上下文的构造函数
        /// </summary>
        public PiscesClientException(PiscesClientCode errorCode, RequestCommand command, string message = null, Exception inner = null) : base(message ?? errorCode.GetMessage(), command.CmdInfo, command.MsgId, inner)
        {
            ErrorCode = errorCode;
        }

        public PiscesClientException(PiscesClientCode errorCode, CmdInfo info, int msgId, string message = null,Exception inner = null) : base(message ?? errorCode.GetMessage(), info, msgId, inner)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 指示该错误是否可以通过重试恢复
        /// </summary>
        public bool IsRecoverable => ErrorCode is PiscesClientCode.Timeout or PiscesClientCode.NotConnected or PiscesClientCode.RateLimited;

        public override string ToString()
        {
            // 格式示例：[PiscesClientException] Code: Timeout | Message: Operation timed out. (Cmd: Connect, MsgId: 1)
            var header = $"[{GetType().Name}] Code: {ErrorCode} | Message: {Message}{GetContextSuffix()}";

            return StackTrace != null
                ? $"{header}{Environment.NewLine}{StackTrace}"
                : header;
        }
    }
}
