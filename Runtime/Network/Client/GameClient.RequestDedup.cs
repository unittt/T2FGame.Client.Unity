using System.Collections.Generic;
using Pisces.Client.Utils;

namespace Pisces.Client.Network
{
    /// <summary>
    /// GameClient - 业务请求去重部分
    /// </summary>
    public partial class GameClient
    {
        /// <summary>
        /// 已锁定的路由集合
        /// </summary>
        private readonly HashSet<int> _lockedRoutes = new();

        /// <summary>
        /// 尝试锁定路由
        /// </summary>
        /// <param name="cmdMerge">路由标识</param>
        /// <returns>是否锁定成功</returns>
        private bool TryLockRoute(int cmdMerge)
        {
            if (!_options.EnableRequestDedup)
                return true;

            if (_options.DedupExcludeList.Contains(cmdMerge))
                return true;

            return _lockedRoutes.Add(cmdMerge);
        }

        /// <summary>
        /// 解锁路由
        /// </summary>
        /// <param name="cmdMerge">路由标识</param>
        private void UnlockRoute(int cmdMerge)
        {
            _lockedRoutes.Remove(cmdMerge);
        }

        /// <summary>
        /// 清理所有路由锁定
        /// </summary>
        private void ClearLockedRoutes()
        {
            _lockedRoutes.Clear();
            GameLogger.LogDebug("[GameClient] 已清理所有路由锁定");
        }
    }
}
