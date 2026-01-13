using System.Collections.Generic;
using Pisces.Client.Network.Channel;
using Pisces.Client.Settings;
using Pisces.Client.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pisces.Client.Editor.Settings
{
    /// <summary>
    /// Pisces Client SDK Project Settings Provider
    /// 在 Edit -> Project Settings -> Pisces Client 中显示
    /// </summary>
    public class PiscesSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/Pisces Client";
        private const string WebSocketDefineSymbol = "ENABLE_WEBSOCKET";
        
        private SerializedObject _serializedSettings;
        private PiscesSettings _settings;

        // Serialized Properties
        private SerializedProperty _serverEnvironments;
        private SerializedProperty _activeEnvironmentIndex;
        private SerializedProperty _channelType;
        private SerializedProperty _connectTimeoutMs;
        private SerializedProperty _requestTimeoutMs;
        private SerializedProperty _heartbeatIntervalSec;
        private SerializedProperty _heartbeatTimeoutCount;
        private SerializedProperty _autoReconnect;
        private SerializedProperty _reconnectIntervalSec;
        private SerializedProperty _maxReconnectCount;
        private SerializedProperty _receiveBufferSize;
        private SerializedProperty _sendBufferSize;
        private SerializedProperty _enableRateLimit;
        private SerializedProperty _maxSendRate;
        private SerializedProperty _maxBurstSize;
        private SerializedProperty _logLevel;
        private SerializedProperty _useWorkerThread;

        // Foldout States
        private bool _serverFoldout = true;
        private bool _networkFoldout = true;
        private bool _heartbeatFoldout = true;
        private bool _reconnectFoldout = true;
        private bool _bufferFoldout;
        private bool _rateLimitFoldout = true;
        private bool _debugFoldout = true;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _environmentBoxStyle;
        private GUIStyle _activeEnvironmentBoxStyle;

        public PiscesSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
            keywords = new HashSet<string>(new[]
            {
                "Pisces", "Network", "TCP", "UDP", "WebSocket", "Client", "Server",
                "Heartbeat", "Reconnect", "Timeout", "Buffer", "网络", "服务器", "心跳", "重连"
            });
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            _settings =  PiscesSettings.Instance;
            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
                CacheSerializedProperties();
            }
        }

        private void CacheSerializedProperties()
        {
            _serverEnvironments = _serializedSettings.FindProperty("_serverEnvironments");
            _activeEnvironmentIndex = _serializedSettings.FindProperty("_activeEnvironmentIndex");
            _channelType = _serializedSettings.FindProperty("_channelType");
            _connectTimeoutMs = _serializedSettings.FindProperty("_connectTimeoutMs");
            _requestTimeoutMs = _serializedSettings.FindProperty("_requestTimeoutMs");
            _heartbeatIntervalSec = _serializedSettings.FindProperty("_heartbeatIntervalSec");
            _heartbeatTimeoutCount = _serializedSettings.FindProperty("_heartbeatTimeoutCount");
            _autoReconnect = _serializedSettings.FindProperty("_autoReconnect");
            _reconnectIntervalSec = _serializedSettings.FindProperty("_reconnectIntervalSec");
            _maxReconnectCount = _serializedSettings.FindProperty("_maxReconnectCount");
            _receiveBufferSize = _serializedSettings.FindProperty("_receiveBufferSize");
            _sendBufferSize = _serializedSettings.FindProperty("_sendBufferSize");
            _enableRateLimit = _serializedSettings.FindProperty("_enableRateLimit");
            _maxSendRate = _serializedSettings.FindProperty("_maxSendRate");
            _maxBurstSize = _serializedSettings.FindProperty("_maxBurstSize");
            _logLevel = _serializedSettings.FindProperty("_logLevel");
            _useWorkerThread = _serializedSettings.FindProperty("_useWorkerThread");
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }

            if (_environmentBoxStyle == null)
            {
                _environmentBoxStyle = new GUIStyle("helpbox")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }

            if (_activeEnvironmentBoxStyle == null)
            {
                _activeEnvironmentBoxStyle = new GUIStyle("helpbox")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null || _settings == null)
            {
                InitializeSettings();
            }

            InitializeStyles();
            _serializedSettings.Update();

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("恢复默认设置", GUILayout.Width(120)))
                {
                    if (EditorUtility.DisplayDialog("恢复默认设置",
                        "确定要将所有设置恢复为默认值吗？",
                        "确定", "取消"))
                    {
                        _settings.ResetToDefaults();
                        EditorUtility.SetDirty(_settings);
                        _serializedSettings.Update();
                    }
                }
            }

            EditorGUILayout.Space(10);

            // Draw sections
            DrawServerEnvironmentSection();
            DrawNetworkSection();
            DrawHeartbeatSection();
            DrawReconnectSection();
            DrawBufferSection();
            DrawRateLimitSection();
            DrawDebugSection();

            // Validation
            DrawValidationSection();

            _serializedSettings.ApplyModifiedProperties();
        }
        

        private void DrawServerEnvironmentSection()
        {
            _serverFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_serverFoldout, "服务器环境");
            if (_serverFoldout)
            {
                EditorGUI.indentLevel++;

                // Environment selector
                if (_serverEnvironments != null && _serverEnvironments.arraySize > 0)
                {
                    var envNames = new string[_serverEnvironments.arraySize];
                    for (int i = 0; i < _serverEnvironments.arraySize; i++)
                    {
                        var env = _serverEnvironments.GetArrayElementAtIndex(i);
                        var name = env.FindPropertyRelative("Name").stringValue;
                        var host = env.FindPropertyRelative("Host").stringValue;
                        var port = env.FindPropertyRelative("Port").intValue;
                        envNames[i] = $"{name} ({host}:{port})";
                    }

                    EditorGUILayout.Space(5);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("当前环境", GUILayout.Width(150));
                        _activeEnvironmentIndex.intValue = EditorGUILayout.Popup(
                            _activeEnvironmentIndex.intValue, envNames);
                    }

                    // Show active environment details
                    if (_activeEnvironmentIndex.intValue >= 0 &&
                        _activeEnvironmentIndex.intValue < _serverEnvironments.arraySize)
                    {
                        var activeEnv = _serverEnvironments.GetArrayElementAtIndex(_activeEnvironmentIndex.intValue);
                        var desc = activeEnv.FindPropertyRelative("Description").stringValue;
                        if (!string.IsNullOrEmpty(desc))
                        {
                            EditorGUILayout.HelpBox(desc, MessageType.None);
                        }
                    }
                }

                EditorGUILayout.Space(10);

                // Environment list
                EditorGUILayout.LabelField("环境列表", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                for (int i = 0; i < _serverEnvironments.arraySize; i++)
                {
                    DrawEnvironmentItem(i);
                }

                // Add button
                EditorGUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ 添加环境", GUILayout.Width(150)))
                    {
                        _serverEnvironments.InsertArrayElementAtIndex(_serverEnvironments.arraySize);
                        var newEnv = _serverEnvironments.GetArrayElementAtIndex(_serverEnvironments.arraySize - 1);
                        newEnv.FindPropertyRelative("Name").stringValue = "新环境";
                        newEnv.FindPropertyRelative("Host").stringValue = "localhost";
                        newEnv.FindPropertyRelative("Port").intValue = 9090;
                        newEnv.FindPropertyRelative("Description").stringValue = "";
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEnvironmentItem(int index)
        {
            var env = _serverEnvironments.GetArrayElementAtIndex(index);
            var nameProp = env.FindPropertyRelative("Name");
            var hostProp = env.FindPropertyRelative("Host");
            var portProp = env.FindPropertyRelative("Port");
            var descProp = env.FindPropertyRelative("Description");

            var isActive = index == _activeEnvironmentIndex.intValue;

            // 使用不同背景色区分激活状态
            var originalBgColor = GUI.backgroundColor;
            if (isActive)
            {
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f, 1f);
            }

            using (new EditorGUILayout.VerticalScope(_environmentBoxStyle))
            {
                GUI.backgroundColor = originalBgColor;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var displayName = isActive ? "★ " + nameProp.stringValue : nameProp.stringValue;
                    EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (!isActive && GUILayout.Button("设为当前", GUILayout.Width(80)))
                    {
                        _activeEnvironmentIndex.intValue = index;
                    }

                    if (_serverEnvironments.arraySize > 1)
                    {
                        if (GUILayout.Button("×", GUILayout.Width(25)))
                        {
                            if (EditorUtility.DisplayDialog("删除环境",
                                $"确定要删除环境 '{nameProp.stringValue}' 吗？",
                                "删除", "取消"))
                            {
                                _serverEnvironments.DeleteArrayElementAtIndex(index);
                                if (_activeEnvironmentIndex.intValue >= _serverEnvironments.arraySize)
                                {
                                    _activeEnvironmentIndex.intValue = _serverEnvironments.arraySize - 1;
                                }
                                return;
                            }
                        }
                    }
                }

                EditorGUILayout.PropertyField(nameProp, new GUIContent("环境名称"));
                EditorGUILayout.PropertyField(hostProp, new GUIContent("服务器地址"));
                EditorGUILayout.PropertyField(portProp, new GUIContent("端口"));
                EditorGUILayout.PropertyField(descProp, new GUIContent("描述"));
            }
        }

        private void DrawNetworkSection()
        {
            EditorGUILayout.Space(5);
            _networkFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_networkFoldout, "网络设置");
            if (_networkFoldout)
            {
                EditorGUI.indentLevel++;

                // 检测协议类型变化
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_channelType, new GUIContent("传输协议",
                    "传输协议类型（TCP、UDP 或 WebSocket）"));
                if (EditorGUI.EndChangeCheck())
                {
                    // 应用修改以获取最新值
                    _serializedSettings.ApplyModifiedProperties();

                    // 同步 WebSocket 宏定义
                    SyncWebSocketDefineSymbol(_settings.ChannelType);

                    // 重新加载序列化对象
                    _serializedSettings.Update();
                }

                // 显示当前宏状态
                var hasWebSocketDefine = HasScriptingDefineSymbol(WebSocketDefineSymbol);
                var currentChannelType = (ChannelType)_channelType.enumValueIndex;

                if (currentChannelType == ChannelType.WebSocket)
                {
                    if (hasWebSocketDefine)
                    {
                        EditorGUILayout.HelpBox($"已启用 {WebSocketDefineSymbol} 宏定义", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"需要 {WebSocketDefineSymbol} 宏定义，点击下方按钮添加", MessageType.Warning);
                        if (GUILayout.Button($"添加 {WebSocketDefineSymbol} 宏定义"))
                        {
                            AddScriptingDefineSymbol(WebSocketDefineSymbol);
                        }
                    }
                }
                else if (hasWebSocketDefine)
                {
                    EditorGUILayout.HelpBox($"当前未使用 WebSocket，但 {WebSocketDefineSymbol} 宏定义仍存在", MessageType.Info);
                    if (GUILayout.Button($"移除 {WebSocketDefineSymbol} 宏定义"))
                    {
                        RemoveScriptingDefineSymbol(WebSocketDefineSymbol);
                    }
                }

                EditorGUILayout.Space(5);

                DrawSliderWithInput(_connectTimeoutMs, 1000, 60000, "连接超时 (毫秒)",
                    "等待连接建立的最大时间");

                DrawSliderWithInput(_requestTimeoutMs, 1000, 120000, "请求超时 (毫秒)",
                    "等待服务器响应的最大时间");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawHeartbeatSection()
        {
            EditorGUILayout.Space(5);
            _heartbeatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_heartbeatFoldout, "心跳设置");
            if (_heartbeatFoldout)
            {
                EditorGUI.indentLevel++;

                DrawSliderWithInput(_heartbeatIntervalSec, 5, 120, "心跳间隔 (秒)",
                    "发送心跳包的间隔时间");

                DrawSliderWithInput(_heartbeatTimeoutCount, 1, 10, "超时次数",
                    "连续多少次心跳超时后断开连接");

                // Calculate and display effective timeout
                var effectiveTimeout = _heartbeatIntervalSec.intValue * _heartbeatTimeoutCount.intValue;
                EditorGUILayout.HelpBox(
                    $"实际断线超时时间: {effectiveTimeout} 秒",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawReconnectSection()
        {
            EditorGUILayout.Space(5);
            _reconnectFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_reconnectFoldout, "重连设置");
            if (_reconnectFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_autoReconnect, new GUIContent("自动重连",
                    "断线后自动尝试重新连接"));

                using (new EditorGUI.DisabledGroupScope(!_autoReconnect.boolValue))
                {
                    DrawSliderWithInput(_reconnectIntervalSec, 1, 30, "重连间隔 (秒)",
                        "两次重连尝试之间的间隔");

                    DrawSliderWithInput(_maxReconnectCount, 0, 100, "最大重连次数",
                        "最大重连尝试次数（0 = 无限）");

                    if (_maxReconnectCount.intValue == 0)
                    {
                        EditorGUILayout.HelpBox("已启用无限重连", MessageType.Warning);
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawBufferSection()
        {
            EditorGUILayout.Space(5);
            _bufferFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_bufferFoldout, "缓冲区设置（高级）");
            if (_bufferFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "缓冲区大小会影响内存使用和吞吐量。默认值（64KB）适用于大多数游戏。",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("接收缓冲区", GUILayout.Width(150));
                    _receiveBufferSize.intValue = EditorGUILayout.IntField(_receiveBufferSize.intValue);
                    EditorGUILayout.LabelField(FormatBytes(_receiveBufferSize.intValue), GUILayout.Width(80));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("发送缓冲区", GUILayout.Width(150));
                    _sendBufferSize.intValue = EditorGUILayout.IntField(_sendBufferSize.intValue);
                    EditorGUILayout.LabelField(FormatBytes(_sendBufferSize.intValue), GUILayout.Width(80));
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawRateLimitSection()
        {
            EditorGUILayout.Space(5);
            _rateLimitFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_rateLimitFoldout, "流量控制");
            if (_rateLimitFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_enableRateLimit, new GUIContent("启用限流",
                    "防止消息发送过快导致网络拥塞"));

                using (new EditorGUI.DisabledGroupScope(!_enableRateLimit.boolValue))
                {
                    EditorGUILayout.Space(5);

                    DrawSliderWithInput(_maxSendRate, 10, 1000, "每秒最大消息数",
                        "持续发送速率上限（消息/秒）");

                    DrawSliderWithInput(_maxBurstSize, 10, 200, "最大突发消息数",
                        "允许短时间突发的最大消息数");

                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox(
                        $"当前配置: 持续发送速率 {_maxSendRate.intValue}/秒，允许突发 {_maxBurstSize.intValue} 条消息",
                        MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.Space(5);
            _debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_debugFoldout, "调试设置");
            if (_debugFoldout)
            {
                EditorGUI.indentLevel++;

                // 日志级别下拉选择
                EditorGUILayout.PropertyField(_logLevel, new GUIContent("日志级别",
                    "控制日志输出的详细程度（Off 关闭所有日志）"));

                // 显示日志级别说明
                var currentLevel = (GameLogLevel)_logLevel.enumValueIndex;
                var levelDescription = currentLevel switch
                {
                    GameLogLevel.Verbose => "输出所有日志（包括消息收发细节）",
                    GameLogLevel.Debug => "输出调试及以上级别日志",
                    GameLogLevel.Info => "输出信息及以上级别日志（默认）",
                    GameLogLevel.Warning => "仅输出警告和错误",
                    GameLogLevel.Error => "仅输出错误",
                    GameLogLevel.Off => "关闭所有日志输出",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(levelDescription))
                {
                    EditorGUILayout.HelpBox(levelDescription, MessageType.None);
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(_useWorkerThread, new GUIContent("使用工作线程",
                    "在独立线程处理网络 I/O（WebGL 不支持）"));

#if UNITY_WEBGL || UNITY_WEIXINMINIGAME
                EditorGUILayout.HelpBox(
                    "WebGL / 小游戏平台不支持工作线程",
                    MessageType.Info);
#endif

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.Space(10);

            if (_settings.Validate(out var errors))
            {
                EditorGUILayout.HelpBox("所有设置验证通过", MessageType.Info);
            }
            else
            {
                foreach (var error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
        }

        private void DrawSliderWithInput(SerializedProperty property, int min, int max, string label, string tooltip)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(180));
                property.intValue = EditorGUILayout.IntSlider(property.intValue, min, max);
            }
        }

        private static string FormatBytes(int bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024f:F1} KB";
            return $"{bytes} B";
        }

        #region Settings Asset Management
        [SettingsProvider]
        public static SettingsProvider CreatePiscesSettingsProvider()
        {
            return new PiscesSettingsProvider(SettingsPath);
        }

        #endregion

        #region Scripting Define Symbol Management

        /// <summary>
        /// 获取当前构建目标组
        /// </summary>
        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

        /// <summary>
        /// 检查是否存在指定的宏定义
        /// </summary>
        private static bool HasScriptingDefineSymbol(string symbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            foreach (var define in defines)
            {
                if (define == symbol)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 添加宏定义
        /// </summary>
        private static void AddScriptingDefineSymbol(string symbol)
        {
            if (HasScriptingDefineSymbol(symbol))
                return;

            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            var newDefines = new string[defines.Length + 1];
            defines.CopyTo(newDefines, 0);
            newDefines[defines.Length] = symbol;

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines);
            Debug.Log($"[Pisces] 已添加宏定义: {symbol}");
        }

        /// <summary>
        /// 移除宏定义
        /// </summary>
        private static void RemoveScriptingDefineSymbol(string symbol)
        {
            if (!HasScriptingDefineSymbol(symbol))
                return;

            var namedBuildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);

            var newDefines = new List<string>(defines.Length);
            foreach (var define in defines)
            {
                if (define != symbol)
                    newDefines.Add(define);
            }

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines.ToArray());
            Debug.Log($"[Pisces] 已移除宏定义: {symbol}");
        }

        /// <summary>
        /// 根据通道类型同步 WebSocket 宏定义
        /// </summary>
        private static void SyncWebSocketDefineSymbol(ChannelType channelType)
        {
            if (channelType == ChannelType.WebSocket)
            {
                AddScriptingDefineSymbol(WebSocketDefineSymbol);
            }
            else
            {
                RemoveScriptingDefineSymbol(WebSocketDefineSymbol);
            }
        }

        #endregion
    }
}
