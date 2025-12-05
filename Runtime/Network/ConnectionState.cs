namespace T2FGame.Client.Network
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 重连中
        /// </summary>
        Reconnecting,

        /// <summary>
        /// 已关闭（不再重连）
        /// </summary>
        Closed
    }
}