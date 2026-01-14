using System.Collections.Generic;

namespace Pisces.Client.Network.Core
{
    /// <summary>
    /// Pisces 状态码
    /// </summary>
    public enum PiscesCode
    {
        /// <summary>
        /// 发送
        /// </summary>
        Success,
        /// <summary>
        /// SDK 未初始化
        /// </summary>
        NotInitialized,
        /// <summary>
        /// 客户端已关闭
        /// </summary>
        ClientClosed,
        /// <summary>
        /// 未连接到服务器
        /// </summary>
        NotConnected,
        /// <summary>
        /// 被限流丢弃
        /// </summary>
        RateLimited,
        /// <summary>
        /// 无效的请求指令
        /// </summary>
        InvalidRequestCommand,
        /// <summary>
        /// 通道发送失败
        /// </summary>
        ChannelError,
        /// <summary>
        /// 请求已锁定（重复请求）
        /// </summary>
        RequestLocked,
        /// <summary>
        /// 请求超时
        /// </summary>
        Timeout,
        /// <summary>
        /// 重复的消息ID
        /// </summary>
        DuplicateMsgId,

        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown,
    }

    internal static class PiscesCodeHelper
    {
        private static readonly Dictionary<PiscesCode, string> _resultMapping = new();

        static PiscesCodeHelper()
        {
            Mapping(PiscesCode.Success, "Success");
            Mapping(PiscesCode.NotInitialized, "SDK 未初始化");
            Mapping(PiscesCode.ClientClosed, "客户端已关闭");
            Mapping(PiscesCode.NotConnected, "未连接到服务器");
            Mapping(PiscesCode.InvalidRequestCommand, "无效的请求指令");
            Mapping(PiscesCode.ChannelError, "通道发送失败");
            Mapping(PiscesCode.RateLimited, "发送频率超限");
            Mapping(PiscesCode.RequestLocked, "请求已锁定（重复请求）");
            Mapping(PiscesCode.Timeout, "请求超时");
            Mapping(PiscesCode.DuplicateMsgId, "重复的消息ID");
        }

        private static void Mapping(PiscesCode result, string msg)
        {
            _resultMapping[result] = msg;
        }

        public static string GetMessage(this PiscesCode result)
        {
            if (!_resultMapping.TryGetValue(result, out var msg))
            {
                msg = result.ToString();
            }
            return msg;
        }
    }
}
