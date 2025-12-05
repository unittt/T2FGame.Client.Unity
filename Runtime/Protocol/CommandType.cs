namespace T2FGame.Client.Protocol
{
    /// <summary>
    /// 命令类型枚举
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// 心跳包
        /// </summary>
        Heartbeat = 0,

        /// <summary>
        /// 业务消息
        /// </summary>
        Business = 1
    }
}