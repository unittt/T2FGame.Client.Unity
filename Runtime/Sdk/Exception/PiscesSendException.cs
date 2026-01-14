using System;
using Pisces.Client.Network.Core;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 发送失败异常
    /// 用于封装客户端发送请求时的各种失败情况
    /// </summary>
    public class PiscesSendException : Exception
    {
        /// <summary>
        /// 发送结果
        /// </summary>
        public SendResult Result { get; }

        /// <summary>
        /// 命令标识（可选）
        /// </summary>
        public CmdInfo? CmdInfo { get; }

        /// <summary>
        /// 消息ID（可选）
        /// </summary>
        public int MsgId { get; }

        public PiscesSendException(SendResult result, string message = null) : base(message ?? result.GetMessage())
        {
            Result = result;
        }

        public PiscesSendException(SendResult result, CmdInfo cmdInfo, int msgId = 0, string message = null) : base(message ?? GetDefaultMessage(result, cmdInfo, msgId))
        {
            Result = result;
            CmdInfo = cmdInfo;
            MsgId = msgId;
        }


        private static string GetDefaultMessage(SendResult result, CmdInfo cmdInfo, int msgId)
        {
            var baseMsg = result.GetMessage();
            return $"{baseMsg} (Cmd={cmdInfo}, MsgId={msgId})";
        }

        public override string ToString()
        {
            return CmdInfo.HasValue
                ? $"PiscesSendException: Result={Result}, Cmd={CmdInfo}, MsgId={MsgId}, Message={Message}"
                : $"PiscesSendException: Result={Result}, Message={Message}";
        }
    }
}
