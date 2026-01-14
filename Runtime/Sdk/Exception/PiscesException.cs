using System;
using Pisces.Client.Network.Core;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// Pisces 框架通用异常
    /// 涵盖了发送失败、协议错误、连接中断、超时等所有框架级异常
    /// </summary>
    public class PiscesException : Exception
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public PiscesCode ErrorCode { get; }

        /// <summary>
        /// 命令标识（可选）
        /// </summary>
        public CmdInfo? CmdInfo { get; }

        /// <summary>
        /// 消息ID（可选）
        /// </summary>
        public int MsgId { get; }

        public PiscesException(PiscesCode errorCode, string message = null) : base(message ?? errorCode.GetMessage())
        {
            ErrorCode = errorCode;
        }

        public PiscesException(PiscesCode errorCode, RequestCommand command, string message = null): base(message ?? GetDefaultMessage(errorCode, command.CmdInfo, command.MsgId))
        {
            ErrorCode = errorCode;
            CmdInfo = command.CmdInfo;
            MsgId = command.MsgId;
        }

        public PiscesException(PiscesCode errorCode, CmdInfo cmdInfo, int msgId = 0, string message = null) : base(message ?? GetDefaultMessage(errorCode, cmdInfo, msgId))
        {
            ErrorCode = errorCode;
            CmdInfo = cmdInfo;
            MsgId = msgId;
        }


        private static string GetDefaultMessage(PiscesCode result, CmdInfo cmdInfo, int msgId)
        {
            var baseMsg = result.GetMessage();
            return $"{baseMsg} (Cmd={cmdInfo}, MsgId={msgId})";
        }

        public override string ToString()
        {
            return CmdInfo.HasValue
                ? $"PiscesSendException: Result={ErrorCode}, Cmd={CmdInfo}, MsgId={MsgId}, Message={Message}"
                : $"PiscesSendException: Result={ErrorCode}, Message={Message}";
        }
    }
}
