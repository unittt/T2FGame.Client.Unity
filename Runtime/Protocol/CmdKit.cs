using System.Collections.Generic;
using System.Text;

namespace Pisces.Protocol
{
    /// <summary>
    /// 命令路由工具
    /// </summary>
    public static class CmdKit
    {
        // 路由映射表（cmdMerge -> 描述）
        private static readonly Dictionary<int, string> _cmdMapping = new();
        private static readonly StringBuilder _sb = new(100);

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
        /// 格式化器委托
        /// </summary>
        public delegate string CmdFormatter(int cmd, int subCmd, int mergedCmd);
        public static CmdFormatter CurrentFormatter { get; set; } = DefaultFormatter;

        // 默认格式化器（结合路由映射）
        private static string DefaultFormatter(int cmd, int subCmd, int mergedCmd)
        {
            _sb.Clear();
            _sb.Append(cmd).Append('-')
                .Append(subCmd).Append('-')
                .Append(mergedCmd);

            if (_cmdMapping.TryGetValue(mergedCmd, out var title))
            {
                _sb.Append('(').Append(title).Append(')');
            }
            return _sb.ToString();
        }

        // 字符串表示
        public static string ToString(int mergedCmd)
        {
            var cmd = GetCmd(mergedCmd);
            var subCmd = GetSubCmd(mergedCmd);
            return CurrentFormatter(cmd, subCmd, mergedCmd);
        }

        public static string ToString(int cmd, int subCmd)
        {
            return CurrentFormatter(cmd, subCmd, Merge(cmd, subCmd));
        }

        /// <summary>
        /// 注册映射（由生成代码调用）
        /// </summary>
        /// <returns>返回 cmdMerge 本身，便于链式调用</returns>
        public static int Mapping(int cmdMerge, string description)
        {
            _cmdMapping[cmdMerge] = description;
            return cmdMerge;
        }

        /// <summary>
        /// 清除所有映射（用于热重载）
        /// </summary>
        public static void ClearMappings()
        {
            _cmdMapping.Clear();
        }
    }
}
