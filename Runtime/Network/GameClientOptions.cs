using System;

namespace T2FGame.Client.Network
{
    [Serializable]
    public sealed class GameClientOptions
    {
         /// <summary>
        /// 传输协议类型（默认 TCP）
        /// </summary>
        public TransportType TransportType = TransportType.Tcp;

        /// <summary>
        /// 服务器地址
        /// 对于 WebSocket，可以使用完整 URL（如 ws://host:port 或 wss://host:port）
        /// </summary>
        public string Host  = "localhost";

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
        public int RequestTimeoutMs  = 30000;

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public int HeartbeatIntervalSec  = 30;

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
        public int ReconnectIntervalSec  = 3;

        /// <summary>
        /// 最大重连次数（0=无限）
        /// </summary>
        public int MaxReconnectCount  = 5;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        public int ReceiveBufferSize  = 65536;

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        public int SendBufferSize = 65536;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLog  = true;

        /// <summary>
        /// 是否使用工作线程进行网络收发
        ///
        /// - true: 在独立线程中处理网络IO，避免阻塞主线程（推荐用于 Android/iOS/Windows/Mac/Linux）
        /// - false: 在主线程中处理网络IO（必须用于 WebGL/微信小游戏等不支持多线程的平台）
        ///
        /// 注意：WebSocket 传输不受此配置影响，始终使用异步回调模式
        /// </summary>
        public bool UseWorkerThread { get; set; } =
#if UNITY_WEBGL || UNITY_WEIXINMINIGAME
            false;
#else
            true;
#endif
    }
}