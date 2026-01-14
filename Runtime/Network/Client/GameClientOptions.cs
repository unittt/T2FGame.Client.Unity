using System;
using Pisces.Client.Network.Channel;
using Pisces.Client.Utils;

namespace Pisces.Client.Network
{
    [Serializable]
    public sealed class GameClientOptions
    {
        /// <summary>
        /// 传输协议类型（默认 TCP）
        /// </summary>
        public ChannelType ChannelType;

        /// <summary>
        /// 服务器地址
        /// 对于 WebSocket，可以使用完整 URL（如 ws://host:port 或 wss://host:port）
        /// </summary>
        public string Host = "localhost";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port = 9090;

        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        public int ConnectTimeoutMs = 10000;

        /// <summary>
        /// 请求超时（毫秒）
        /// </summary>
        public int RequestTimeoutMs = 30000;

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public int HeartbeatIntervalSec = 30;

        /// <summary>
        /// 心跳超时次数（超过此次数认为断开连接）
        /// </summary>
        public int HeartbeatTimeoutCount = 3;

        /// <summary>
        /// 是否自动重连
        /// </summary>
        public bool AutoReconnect = true;

        /// <summary>
        /// 重连间隔（秒）
        /// </summary>
        public int ReconnectIntervalSec = 3;

        /// <summary>
        /// 最大重连次数（0=无限）
        /// </summary>
        public int MaxReconnectCount = 5;

        /// <summary>
        /// 接收缓冲区大小（Socket 级别）
        /// </summary>
        public int ReceiveBufferSize = 65536;

        /// <summary>
        /// 发送缓冲区大小（Socket 级别）
        /// </summary>
        public int SendBufferSize = 65536;

        #region PacketBuffer 配置

        /// <summary>
        /// PacketBuffer 初始大小（字节）
        /// 默认 4KB，可满足大多数消息需求
        /// </summary>
        public int PacketBufferInitialSize = 4096;

        /// <summary>
        /// PacketBuffer 收缩阈值
        /// 当容量超过此大小且使用率低于 25% 时，自动收缩
        /// 默认 64KB
        /// </summary>
        public int PacketBufferShrinkThreshold = 65536;

        #endregion

        /// <summary>
        /// 日志级别（默认 Info）
        /// </summary>
        public GameLogLevel LogLevel = GameLogLevel.Info;

        #region 流量控制

        /// <summary>
        /// 是否启用发送限流
        /// </summary>
        public bool EnableRateLimit = true;

        /// <summary>
        /// 每秒最大发送消息数（持续速率）
        /// </summary>
        public int MaxSendRate = 100;

        /// <summary>
        /// 最大突发消息数（桶容量）
        /// 允许短时间内发送的最大消息数，超过后会被限流
        /// </summary>
        public int MaxBurstSize = 50;

        #endregion

        /// <summary>
        /// 克隆配置
        /// </summary>
        public GameClientOptions Clone()
        {
            return new GameClientOptions
            {
                ChannelType = ChannelType,
                Host = Host,
                Port = Port,
                ConnectTimeoutMs = ConnectTimeoutMs,
                RequestTimeoutMs = RequestTimeoutMs,
                HeartbeatIntervalSec = HeartbeatIntervalSec,
                HeartbeatTimeoutCount = HeartbeatTimeoutCount,
                AutoReconnect = AutoReconnect,
                ReconnectIntervalSec = ReconnectIntervalSec,
                MaxReconnectCount = MaxReconnectCount,
                ReceiveBufferSize = ReceiveBufferSize,
                SendBufferSize = SendBufferSize,
                PacketBufferInitialSize = PacketBufferInitialSize,
                PacketBufferShrinkThreshold = PacketBufferShrinkThreshold,
                LogLevel = LogLevel,
                EnableRateLimit = EnableRateLimit,
                MaxSendRate = MaxSendRate,
                MaxBurstSize = MaxBurstSize,
            };
        }
    }
}
