using System;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// Pisces 框架统一异常
    /// 用于封装服务器返回的错误响应
    /// </summary>
    public class PiscesResponseException : Exception
    {
        /// <summary>
        /// 响应状态码（错误码）
        /// </summary>
        public int ResponseStatus { get; }

        /// <summary>
        /// 命令标识
        /// </summary>
        public CmdInfo CmdInfo { get; }

        /// <summary>
        /// 消息ID（用于追踪请求）
        /// </summary>
        public int MsgId { get; }

        /// <summary>
        /// 服务器返回的错误消息
        /// </summary>
        public string ErrorMessage { get; }

        public PiscesResponseException(ResponseMessage response) : base(FormatMessage(response))
        {
            ResponseStatus = response.ResponseStatus;
            CmdInfo = response.CmdInfo;
            MsgId = response.MsgId;
            ErrorMessage = response.ErrorMessage;
        }

        private static string FormatMessage(ResponseMessage response)
        {
            if (string.IsNullOrEmpty(response.ErrorMessage))
                return $"[{response.ResponseStatus}] Request failed: {response.CmdInfo.ToString()} (MsgId={response.MsgId})";
            return $"[{response.ResponseStatus}] {response.ErrorMessage} ({response.CmdInfo.ToString()}, MsgId={response.MsgId})";
        }

        public override string ToString()
        {
            return $"PiscesResponseException: Status={ResponseStatus}, CmdMerge={CmdInfo}, MsgId={MsgId}, Message={ErrorMessage}";
        }
    }
}
