using System;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// Pisces 框架统一异常
    /// 用于封装服务器返回的错误响应
    /// </summary>
    public class PiscesException : Exception
    {
        /// <summary>
        /// 响应状态码（错误码）
        /// </summary>
        public int ResponseStatus { get; }

        /// <summary>
        /// 命令标识
        /// </summary>
        public int CmdMerge { get; }

        /// <summary>
        /// 消息ID（用于追踪请求）
        /// </summary>
        public int MsgId { get; }

        /// <summary>
        /// 服务器返回的错误消息
        /// </summary>
        public string ErrorMessage { get; }

        public PiscesException(ResponseMessage response)
            : base(FormatMessage(response))
        {
            ResponseStatus = response.ResponseStatus;
            CmdMerge = response.CmdMerge;
            MsgId = response.MsgId;
            ErrorMessage = response.ErrorMessage;
        }

        public PiscesException(int responseStatus, string errorMessage, int cmdMerge = 0, int msgId = 0)
            : base($"[{responseStatus}] {errorMessage}")
        {
            ResponseStatus = responseStatus;
            CmdMerge = cmdMerge;
            MsgId = msgId;
            ErrorMessage = errorMessage;
        }

        private static string FormatMessage(ResponseMessage response)
        {
            var cmdInfo = CmdKit.ToString(response.CmdMerge);
            if (string.IsNullOrEmpty(response.ErrorMessage))
                return $"[{response.ResponseStatus}] Request failed: {cmdInfo} (MsgId={response.MsgId})";
            return $"[{response.ResponseStatus}] {response.ErrorMessage} ({cmdInfo}, MsgId={response.MsgId})";
        }

        public override string ToString()
        {
            return $"PiscesException: Status={ResponseStatus}, CmdMerge={CmdMerge}, MsgId={MsgId}, Message={ErrorMessage}";
        }
    }
}
