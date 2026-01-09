using System.Collections.Generic;

namespace Pisces.Protocol
{
    /// <summary>
    /// 命令路由工具
    /// </summary>
    public static class CmdKit
    {
        // 请求映射表（cmdMerge -> 描述）
        private static readonly Dictionary<int, string> _requestMapping = new();

        // 广播映射表（cmdMerge -> 描述）
        private static readonly Dictionary<int, string> _broadcastMapping = new();
        
        /// <summary>
        /// 获取主命令（高16位）
        /// </summary>
        public static int GetCmd(int cmdMerge) => cmdMerge >> 16;

        /// <summary>
        /// 获取子命令（低16位）
        /// </summary>
        public static int GetSubCmd(int cmdMerge) => cmdMerge & 0xFFFF;

        /// <summary>
        /// 合并命令
        /// </summary>
        public static int Merge(int cmd, int subCmd) => (cmd << 16) | subCmd;

        /// <summary>
        /// 格式化为字符串 [cmd-subCmd]
        /// </summary>
        public static string ToString(int cmdMerge) => $"[{GetCmd(cmdMerge)}-{GetSubCmd(cmdMerge)}]";

        /// <summary>
        /// 注册请求映射（由生成代码调用）
        /// </summary>
        /// <returns>返回 cmdMerge 本身，便于链式调用</returns>
        public static int MappingRequest(int cmdMerge, string description)
        {
            _requestMapping[cmdMerge] = description;
            return cmdMerge;
        }

        /// <summary>
        /// 注册广播映射（由生成代码调用）
        /// </summary>
        /// <returns>返回 cmdMerge 本身，便于链式调用</returns>
        public static int MappingBroadcast(int cmdMerge, string description)
        {
            _broadcastMapping[cmdMerge] = description;
            return cmdMerge;
        }
        
        /// <summary>
        /// 获取请求描述
        /// </summary>
        public static string GetRequestTitle(int cmdMerge)
            => _requestMapping.TryGetValue(cmdMerge, out var title) ? title : $"Request{ToString(cmdMerge)}";

        /// <summary>
        /// 获取广播描述
        /// </summary>
        public static string GetBroadcastTitle(int cmdMerge)
        {
            return _broadcastMapping.TryGetValue(cmdMerge, out var title) ? title : $"Broadcast{ToString(cmdMerge)}";
        }
        
        /// <summary>
        /// 清除所有映射（用于热重载）
        /// </summary>
        public static void ClearMappings()
        {
            _requestMapping.Clear();
            _broadcastMapping.Clear();
        }
    }
}
