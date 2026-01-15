using System;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 服务端响应异常
    /// 当请求已成功发送，但服务器返回了表示失败的状态码时抛出
    /// </summary>
    public class PiscesServerException : PiscesException
    {
        /// <summary>
        /// 服务器返回的原始状态码
        /// </summary>
        public int ResponseStatus { get; }

        /// <summary>
        /// 服务器返回的原始错误消息
        /// </summary>
        public string ServerErrorMessage { get; }

        public PiscesServerException(ResponseMessage response) : base(BuildErrorMessage(response), response.CmdInfo, response.MsgId)
        {
            ResponseStatus = response.ResponseStatus;
            ServerErrorMessage = response.ErrorMessage;
        }

        private static string BuildErrorMessage(ResponseMessage response)
        {
            if (string.IsNullOrEmpty(response.ErrorMessage))
                return $"Server returned error status {response.ResponseStatus}.";

            return response.ErrorMessage;
        }

        public override string ToString()
        {
            // 格式示例：[PiscesServerException] Status: 500 | Message: Internal Server Error (Cmd: GetUser, MsgId: 102)
            var header = $"[{GetType().Name}] Status: {ResponseStatus} | Message: {Message}{GetContextSuffix()}";

            return StackTrace != null
                ? $"{header}{Environment.NewLine}{StackTrace}"
                : header;
        }
    }
}
