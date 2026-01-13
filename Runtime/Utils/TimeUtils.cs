using System;

namespace Pisces.Client.Utils
{
    /// <summary>
    /// 时间同步工具类
    /// 提供客户端与服务器时间同步功能
    /// </summary>
    public static class TimeUtils
    {
        private static long _clockOffsetMs;
        private static float _rttMs;
        private static volatile bool _synced;

        /// <summary>
        /// 网络往返延迟（毫秒）
        /// </summary>
        public static float RttMs => _rttMs;

        /// <summary>
        /// 时钟偏移量（毫秒）
        /// 正值表示服务器时间比客户端快，负值表示服务器时间比客户端慢
        /// </summary>
        public static long ClockOffsetMs => _clockOffsetMs;

        /// <summary>
        /// 是否已完成时间同步
        /// </summary>
        public static bool IsSynced => _synced;

        /// <summary>
        /// 获取服务器时间（毫秒时间戳）
        /// 如果未同步，返回本地时间
        /// </summary>
        public static long ServerTimeMs => GetLocalTimeMs() + _clockOffsetMs;

        /// <summary>
        /// 获取服务器时间（DateTime，本地时区）
        /// </summary>
        public static DateTime ServerTime => DateTimeOffset.FromUnixTimeMilliseconds(ServerTimeMs).LocalDateTime;

        /// <summary>
        /// 获取服务器时间（DateTimeOffset，UTC）
        /// </summary>
        public static DateTimeOffset ServerTimeUtc => DateTimeOffset.FromUnixTimeMilliseconds(ServerTimeMs);

        /// <summary>
        /// 获取本地时间戳（毫秒，UTC）
        /// </summary>
        public static long GetLocalTimeMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// 更新时间同步数据
        /// </summary>
        /// <param name="clientTime">客户端发送时间（毫秒时间戳）</param>
        /// <param name="serverTime">服务器时间（毫秒时间戳）</param>
        internal static void UpdateSync(long clientTime, long serverTime)
        {
            var now = GetLocalTimeMs();
            var rtt = now - clientTime;

            // 计算时钟偏移：server_time - (client_time + RTT/2)
            // 即服务器时间减去"客户端发送时间 + 单程延迟"
            var offset = serverTime - clientTime - rtt / 2;

            _rttMs = rtt;
            _clockOffsetMs = offset;
            _synced = true;

            GameLogger.LogDebug($"[TimeUtils] 时间同步完成: RTT={rtt}ms, ClockOffset={offset}ms");
        }

        /// <summary>
        /// 重置时间同步状态
        /// </summary>
        internal static void Reset()
        {
            _clockOffsetMs = 0;
            _rttMs = 0;
            _synced = false;
        }
    }
}
