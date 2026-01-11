using Pisces.Client.Settings;
using UnityEditor;
using UnityEngine;

namespace Pisces.Client.Editor
{
    /// <summary>
    /// Pisces Client SDK 菜单项
    /// </summary>
    public static class PiscesMenuItems
    {
        private const string MenuRoot = "Tools/Pisces Client/";
        private const string SettingsAssetPath = "Assets/Pisces.Client.Unity/Resources/PiscesSettings.asset";

        /// <summary>
        /// 打开 Project Settings
        /// </summary>
        [MenuItem(MenuRoot + "打开设置", priority = 0)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Pisces Client");
        }

        /// <summary>
        /// 选中 Settings 资源文件
        /// </summary>
        [MenuItem(MenuRoot + "定位配置文件", priority = 1)]
        public static void SelectSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PiscesSettings>(SettingsAssetPath);
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                if (EditorUtility.DisplayDialog("未找到配置文件",
                    "未找到 PiscesSettings 配置文件，是否创建？",
                    "创建", "取消"))
                {
                    OpenSettings();
                }
            }
        }

        [MenuItem(MenuRoot + "定位配置文件", true)]
        public static bool ValidateSelectSettingsAsset()
        {
            return AssetDatabase.LoadAssetAtPath<PiscesSettings>(SettingsAssetPath) != null;
        }

        /// <summary>
        /// 重置配置为默认值
        /// </summary>
        [MenuItem(MenuRoot + "恢复默认设置", priority = 20)]
        public static void ResetToDefaults()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PiscesSettings>(SettingsAssetPath);
            if (settings != null)
            {
                if (EditorUtility.DisplayDialog("恢复默认设置",
                    "确定要将所有 Pisces Client 设置恢复为默认值吗？",
                    "确定", "取消"))
                {
                    settings.ResetToDefaults();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Pisces] 已恢复默认设置");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("未找到配置文件",
                    "未找到 PiscesSettings 配置文件，请先通过 Project Settings 创建。",
                    "确定");
            }
        }

        [MenuItem(MenuRoot + "恢复默认设置", true)]
        public static bool ValidateResetToDefaults()
        {
            return AssetDatabase.LoadAssetAtPath<PiscesSettings>(SettingsAssetPath) != null;
        }

        /// <summary>
        /// 打开文档
        /// </summary>
        [MenuItem(MenuRoot + "文档", priority = 100)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/PiscesGameDev/Pisces.Client.Unity#readme");
        }

        /// <summary>
        /// 显示关于信息
        /// </summary>
        [MenuItem(MenuRoot + "关于", priority = 101)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("关于 Pisces Client SDK",
                "Pisces Client SDK v0.0.1\n\n" +
                "高性能、跨平台的 Unity 网络通信框架。\n\n" +
                "功能特性:\n" +
                "- TCP/UDP/WebSocket 多协议支持\n" +
                "- 自动心跳保活与断线重连\n" +
                "- Protobuf 序列化\n" +
                "- UniTask 异步编程\n\n" +
                "Copyright 2024 PiscesGameDev",
                "确定");
        }
    }
}
