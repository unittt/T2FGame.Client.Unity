namespace Pisces.Client.Utils
{
    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// 输出详细日志（消息收发细节）
        /// </summary>
        void LogVerbose(string message);

        /// <summary>
        /// 输出调试日志（状态变化、事件等）
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        /// 输出信息日志（关键节点）
        /// </summary>
        void Log(string message);

        /// <summary>
        /// 输出警告日志
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// 输出错误日志
        /// </summary>
        void LogError(string message);
    }
}
