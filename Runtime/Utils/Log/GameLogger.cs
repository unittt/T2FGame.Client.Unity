namespace Pisces.Client.Utils
{
    /// <summary>
    /// 游戏日志工具
    /// 支持日志分级控制
    /// </summary>
    public static class GameLogger
    {
        private static ILog _logger = new DefaultLog();

        /// <summary>
        /// 日志级别（默认 Info，只输出 Info 及以上级别）
        /// 设置为 Off 可关闭所有日志
        /// </summary>
        public static GameLogLevel Level { get; set; } = GameLogLevel.Info;

        /// <summary>
        /// 设置自定义日志实现
        /// </summary>
        public static void SetLog(ILog logger)
        {
            if (logger != null)
            {
                _logger = logger;
            }
        }

        /// <summary>
        /// 输出详细日志（消息收发细节、序列化数据等）
        /// </summary>
        public static void LogVerbose(string message)
        {
            if (Level <= GameLogLevel.Verbose)
            {
                _logger.LogVerbose(message);
            }
        }

        /// <summary>
        /// 输出调试日志（状态变化、连接事件等）
        /// </summary>
        public static void LogDebug(string message)
        {
            if (Level <= GameLogLevel.Debug)
            {
                _logger.LogDebug(message);
            }
        }

        /// <summary>
        /// 输出信息日志（连接成功、断开等关键节点）
        /// </summary>
        public static void Log(string message)
        {
            if (Level <= GameLogLevel.Info)
            {
                _logger.Log(message);
            }
        }

        /// <summary>
        /// 输出警告日志（重连、超时等）
        /// </summary>
        public static void LogWarning(string message)
        {
            if (Level <= GameLogLevel.Warning)
            {
                _logger.LogWarning(message);
            }
        }

        /// <summary>
        /// 输出错误日志（异常、失败等）
        /// </summary>
        public static void LogError(string message)
        {
            if (Level <= GameLogLevel.Error)
            {
                _logger.LogError(message);
            }
        }

        /// <summary>
        /// 检查指定级别是否会被输出
        /// </summary>
        public static bool IsLevelEnabled(GameLogLevel level)
        {
            return Level <= level;
        }
    }
}
