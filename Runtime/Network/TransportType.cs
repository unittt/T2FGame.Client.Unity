namespace T2FGame.Client.Network
{
    /// <summary>
    /// 传输协议类型
    /// </summary>
    public enum TransportType
    {
        /// <summary>
        /// TCP 传输协议（默认）
        /// 可靠、有序、适用于需要保证消息送达的场景
        /// </summary>
        Tcp = 0,

        /// <summary>
        /// UDP 传输协议
        /// 不可靠、无序、低延迟、适用于实时性要求高的场景（如位置同步）
        /// </summary>
        Udp = 1,

        /// <summary>
        /// WebSocket 传输协议
        /// 可靠、有序、适用于 WebGL 平台或需要穿透防火墙的场景
        /// </summary>
        WebSocket = 2
    }
}