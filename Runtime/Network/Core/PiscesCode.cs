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
        /// 未连接到服务器
        /// </summary>
        NotConnected,
        /// <summary>
        /// 被限流丢弃
        /// </summary>
        RateLimited,
        /// <summary>
        /// 客户端已关闭
        /// </summary>
        ClientClosed,
        /// <summary>
        /// 无效的消息
        /// </summary>
        InvalidMessage,
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
        DuplicateMsgId
    }

    internal static class PiscesCodeHelper
    {
        private static readonly Dictionary<PiscesCode, string> _resultMapping = new();

        static PiscesCodeHelper()
        {
            Mapping(PiscesCode.Success, "Success");
            Mapping(PiscesCode.ClientClosed, "客户端已关闭");
            Mapping(PiscesCode.NotConnected, "未连接到服务器");
            Mapping(PiscesCode.InvalidMessage, "无效的消息");
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
            _resultMapping.TryGetValue(result, out var msg);
            return msg;
        }
    }
}
