using System;
using T2FGame.Client.Utils;

namespace T2FGame.Client.Network.Channel
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
                ChannelType.WebSocket => new WebSocketChannel(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(channelType),
                    channelType,
                    "Unsupported channel type"
                ),
            };

            GameLogger.Log($"[ChannelFactory] 已创建 {channelType} 通道");
            return channel;
        }

        /// <summary>
        /// 创建并初始化通道实例
        /// </summary>
        /// <param name="channelType">通道类型</param>
        /// <returns>已初始化的通道实例</returns>
        public static IProtocolChannel CreateAndInit(ChannelType channelType)
        {
            var channel = Create(channelType);
            channel.OnInit();
            return channel;
        }

        /// <summary>
        /// 创建、初始化并连接通道实例
        /// </summary>
        /// <param name="channelType">通道类型</param>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        /// <returns>已连接的通道实例</returns>
        public static IProtocolChannel CreateAndConnect(
            ChannelType channelType,
            string host,
            int port
        )
        {
            var channel = CreateAndInit(channelType);
            channel.Connect(host, port);
            return channel;
        }
    }
}
