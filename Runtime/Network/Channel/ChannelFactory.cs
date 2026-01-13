using System;
using Pisces.Client.Utils;

namespace Pisces.Client.Network.Channel
{
    /// <summary>
    /// 通道工厂
    /// 用于根据配置创建对应类型的通道实例
    /// </summary>
    public static class ChannelFactory
    {
        /// <summary>
        /// 创建通道实例
        /// </summary>
        /// <param name="channelType">通道类型</param>
        /// <returns>通道实例</returns>
        /// <exception cref="ArgumentOutOfRangeException">不支持的通道类型</exception>
        public static IProtocolChannel Create(ChannelType channelType)
        {
            IProtocolChannel channel = channelType switch
            {
                ChannelType.Tcp => new TcpChannel(),
                ChannelType.Udp => new UdpChannel(),
#if ENABLE_WEBSOCKET
                ChannelType.WebSocket => new WebSocketChannel(),
#endif
                _ => throw new ArgumentOutOfRangeException(
                    nameof(channelType),
                    channelType,
                    "Unsupported channel type"
                ),
            };

            GameLogger.LogDebug($"[ChannelFactory] 创建 {channelType} 通道");
            return channel;
        }
    }
}
