using System.Collections.Generic;

namespace Pisces.Client.Network.Core
{
    /// <summary>
    /// 消息发送结果
    /// </summary>
    public enum SendResult
    {
        /// <summary>
        /// 发送成功（已加入发送队列）
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

    internal static class SendResultHelper
    {
        private static readonly Dictionary<SendResult, string> _resultMapping = new();

        static SendResultHelper()
        {
            Mapping(SendResult.Success, "Success");
            Mapping(SendResult.ClientClosed, "客户端已关闭");
            Mapping(SendResult.NotConnected, "未连接到服务器");
            Mapping(SendResult.InvalidMessage, "无效的消息");
            Mapping(SendResult.ChannelError, "通道发送失败");
            Mapping(SendResult.RateLimited, "发送频率超限");
            Mapping(SendResult.RequestLocked, "请求已锁定（重复请求）");
            Mapping(SendResult.Timeout, "请求超时");
            Mapping(SendResult.DuplicateMsgId, "重复的消息ID");
        }

        private static void Mapping(SendResult result, string msg)
        {
            _resultMapping[result] = msg;
        }

        public static string GetMessage(this SendResult result)
        {
            _resultMapping.TryGetValue(result, out var msg);
            return msg;
        }
    }
}
