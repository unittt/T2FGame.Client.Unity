using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using UnityEngine;

namespace Pisces.Client.Unity
{
    /// <summary>
    /// Pisces SDK Unity 生命周期管理器
    /// 自动处理 Unity 退出时的资源清理
    /// </summary>
    internal static class PiscesLifecycleManager
    {

        private static bool _isPlaying;

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public static bool IsPlaying => _isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _isPlaying = true;
            // 订阅退出事件
            Application.quitting += OnApplicationQuitting;

#if UNITY_EDITOR
            // 编辑器模式下额外处理 Play Mode 退出
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private static void OnApplicationQuitting()
        {
            _isPlaying = false;
            CleanupSdk();
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                _isPlaying = false;
                CleanupSdk();
            }
            else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                _isPlaying = true;
            }
        }
#endif

        private static void CleanupSdk()
        {
            try
            {
                var piscesSdk = PiscesSdk.Instance;
                // 关闭 SDK 连接
                if (!piscesSdk.IsInitialized) return;
                piscesSdk.Close();
                piscesSdk.Dispose();
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError($"[PiscesLifecycleManager] 清理 SDK 时出错: {ex.Message}");
            }
        }
    }
}
