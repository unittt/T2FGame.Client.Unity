using UnityEngine;

namespace Pisces.Client.Utils
{
    /// <summary>
    /// 默认日志实现（基于 Unity Debug）
    /// </summary>
    internal sealed class DefaultLog : ILog
    {
        private const string VerbosePrefix = "[Verbose] ";
        private const string DebugPrefix = "[Debug] ";

        public void LogVerbose(string message)
        {
            Debug.Log(VerbosePrefix + message);
        }

        public void LogDebug(string message)
        {
            Debug.Log(DebugPrefix + message);
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
