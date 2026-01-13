namespace Pisces.Client.Utils
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum GameLogLevel
    {
        /// <summary>
        /// 详细信息（消息收发细节、序列化数据等）
        /// </summary>
        Verbose = 0,

        /// <summary>
        /// 调试信息（状态变化、连接事件等）
        /// </summary>
        Debug = 1,

        /// <summary>
        /// 一般信息（连接成功、断开等关键节点）
        /// </summary>
        Info = 2,

        /// <summary>
        /// 警告信息（重连、超时等）
        /// </summary>
        Warning = 3,

        /// <summary>
        /// 错误信息（异常、失败等）
        /// </summary>
        Error = 4,

        /// <summary>
        /// 关闭所有日志
        /// </summary>
        Off = 5
    }
}
