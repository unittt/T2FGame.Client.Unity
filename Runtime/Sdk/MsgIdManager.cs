using System.Threading;

namespace T2FGame.Client.Sdk
{
    /// <summary>
    /// 消息ID管理器
    /// </summary>
    internal static class MsgIdManager
    {
        private static int _nextMsgId;
        private const int MaxMsgId = int.MaxValue - 10000; // 留出缓冲空间

        /// <summary>
        /// 生成下一个消息ID（线程安全）
        /// </summary>
        public static int GenerateNextMsgId()
        {
            var newId = Interlocked.Increment(ref _nextMsgId);
            // 检查是否接近溢出，如果是则重置
            if (newId < MaxMsgId)
                return newId;
            // 使用 CompareExchange 原子性地重置计数器
            Interlocked.CompareExchange(ref _nextMsgId, 1, newId);
            // 如果重置失败（其他线程已重置），继续使用新值
            newId = Interlocked.Increment(ref _nextMsgId);
            return newId;
        }
    }
}
