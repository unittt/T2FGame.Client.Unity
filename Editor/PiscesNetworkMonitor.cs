using System.Collections.Generic;
using Pisces.Client.Network;
using Pisces.Client.Network.Core;
using Pisces.Client.Settings;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using UnityEditor;
using UnityEngine;

namespace Pisces.Client.Editor
{
    public class PiscesNetworkMonitor : EditorWindow
    {
        // 窗口状态
        private Vector2 _mainScrollPosition;
        private Vector2 _logScrollPosition;
        private string _logFilter = "";
        private bool _autoScroll = true;
        private int _logTypeFilter;

        // 刷新控制
        private double _lastRepaintTime;
        private const double RepaintInterval = 0.1;

        // 缓存
        private readonly List<NetworkMessageLog> _cachedLogs = new();
        private int _lastLogCount;

        // 样式系统
        private GUIStyle _statusConnectedStyle;
        private GUIStyle _statusDisconnectedStyle;
        private GUIStyle _statusReconnectingStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;

        // Log 专用对齐样式
        private GUIStyle _logTimeStyle;
        private GUIStyle _logIdStyle;
        private GUIStyle _logMainStyle;
        private GUIStyle _logInfoStyle;
        private GUIStyle _logErrorStyle;

        private bool _stylesInitialized;

        // 配色方案
        private static readonly Color ColorSendBg = new Color(0.2f, 0.45f, 0.7f, 0.15f); // 柔和蓝
        private static readonly Color ColorRecvBg = new Color(0.25f, 0.55f, 0.4f, 0.15f); // 柔和绿
        private static readonly Color ColorErrBg  = new Color(0.7f, 0.25f, 0.25f, 0.25f); // 警告红
        private static readonly Color ZebraOverlay = new Color(1f, 1f, 1f, 0.05f);        // 斑马纹亮度叠加

        // Foldout
        private bool _connectionFoldout = true;
        private bool _statisticsFoldout = true;
        private bool _heartbeatFoldout = true;
        private bool _timeSyncFoldout = true;
        private bool _logFoldout = true;

        [MenuItem("Tools/Pisces Client/网络监控", priority = 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<PiscesNetworkMonitor>();
            window.titleContent = new GUIContent("网络监控", EditorGUIUtility.IconContent("d_Profiler.NetworkMessages").image);
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        private void OnEnable() => EditorApplication.update += OnEditorUpdate;
        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)
            {
                _lastRepaintTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            const float rowHeight = 22f;

            // 基础样式
            _statusConnectedStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }, fontSize = 14 };
            _statusDisconnectedStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.8f, 0.4f, 0.4f) }, fontSize = 14 };
            _statusReconnectingStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.7f, 0.3f) }, fontSize = 14 };
            _boxStyle = new GUIStyle("helpbox") { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(5, 5, 5, 5) };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            // 日志列样式 (核心：居中对齐 + 固定高度)
            _logTimeStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleLeft, fixedHeight = rowHeight,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };

            _logIdStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleLeft, fixedHeight = rowHeight,
                normal = { textColor = new Color(0.45f, 0.75f, 0.95f, 0.9f) }
            };

            _logMainStyle = new GUIStyle(EditorStyles.label) {
                richText = true, alignment = TextAnchor.MiddleLeft, fixedHeight = rowHeight, fontSize = 11
            };

            _logInfoStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleRight, fixedHeight = rowHeight,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            _logErrorStyle = new GUIStyle(EditorStyles.miniLabel) {
                richText = true, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) },
                padding = new RectOffset(20, 0, 0, 0) // 缩进显示
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (!Application.isPlaying) { DrawNotPlayingUI(); return; }
            if (!PiscesSdk.Instance.IsInitialized) { DrawNotInitializedUI(); return; }

            var client = PiscesSdk.Instance.Client;
            var stats = client.Statistics;
            stats.UpdateRates();

            DrawToolbar(client, stats);
            EditorGUILayout.Space(5);

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_mainScrollPosition))
            {
                _mainScrollPosition = scrollView.scrollPosition;
                DrawConnectionSection(client, client.Options);
                DrawStatisticsSection(stats);
                DrawHeartbeatSection(stats, client.Options);
                DrawTimeSyncSection();
                DrawLogSection(stats);
            }
        }

        private void DrawLogSection(NetworkStatistics stats)
        {
            _logFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_logFoldout, $"消息日志 ({stats.LogCount})");
            if (_logFoldout)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("过滤:", GUILayout.Width(35));
                    _logTypeFilter = EditorGUILayout.Popup(_logTypeFilter, new[] { "全部", "发送 ↑", "接收 ↓" }, GUILayout.Width(70));
                    _logFilter = EditorGUILayout.TextField(_logFilter, EditorStyles.toolbarSearchField);
                    _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", GUILayout.Width(70));
                    if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    {
                        stats.ClearLogs();
                        _cachedLogs.Clear();
                    }
                }

                if (stats.LogCount != _lastLogCount)
                {
                    stats.GetLogs(_cachedLogs);
                    _lastLogCount = stats.LogCount;
                }

                var logAreaHeight = Mathf.Max(200, position.height - 480);
                using (var scrollView = new EditorGUILayout.ScrollViewScope(_logScrollPosition, "box", GUILayout.Height(logAreaHeight)))
                {
                    _logScrollPosition = scrollView.scrollPosition;

                    for (int i = 0; i < _cachedLogs.Count; i++)
                    {
                        var log = _cachedLogs[i];
                        if (_logTypeFilter == 1 && !log.IsOutgoing) continue;
                        if (_logTypeFilter == 2 && log.IsOutgoing) continue;
                        if (!string.IsNullOrEmpty(_logFilter) && !log.CmdDisplay.Contains(_logFilter)) continue;

                        DrawLogEntry(log, i);
                    }

                    if (_autoScroll && Event.current.type == EventType.Repaint)
                    {
                        _logScrollPosition.y = float.MaxValue;
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLogEntry(NetworkMessageLog log, int index)
        {
            // 1. 动态计算背景色
            Color bgColor = log.IsOutgoing ? ColorSendBg : ColorRecvBg;
            if (!log.IsSuccess) bgColor = ColorErrBg;
            if (index % 2 == 0) bgColor += ZebraOverlay; // 斑马纹处理

            // 2. 绘制行背景
            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
            var prevColor = GUI.color;
            GUI.color = bgColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = prevColor;

            // 3. 绘制列数据 (确保样式中 alignment 已经设为 MiddleLeft/Right)

            // [时间] 85px
            GUILayout.Label(log.Timestamp.ToString("HH:mm:ss.fff"), _logTimeStyle, GUILayout.Width(85));

            // [方向] 18px
            string arrow = log.IsOutgoing ? "<color=#569cd6>↑</color>" : "<color=#4ec9b0>↓</color>";
            GUILayout.Label(arrow, _logMainStyle, GUILayout.Width(18));

            // [MsgId] 45px
            string idStr = log.MsgId != 0 ? $"#{log.MsgId:D3}" : " ---";
            GUILayout.Label(idStr, _logIdStyle, GUILayout.Width(45));

            // [协议名] 自动扩展
            string cmdColor = log.IsSuccess ? "#dcdcdc" : "#f44747";
            string cmdStr = $"<color={cmdColor}>{log.CmdDisplay}</color>";
            if (log.IsBroadcast) cmdStr += " <color=#4fc1ff>[广播]</color>";
            GUILayout.Label(cmdStr, _logMainStyle, GUILayout.ExpandWidth(true));

            // [大小] 50px
            GUILayout.Label($"{log.DataSize} B", _logInfoStyle, GUILayout.Width(50));

            // [耗时] 65px
            if (!log.IsOutgoing && log.ElapsedMs.HasValue)
            {
                float elapsed = log.ElapsedMs.Value;
                string color = elapsed < 100 ? "#6a9955" : elapsed < 500 ? "#d7ba7d" : "#f44747";
                GUILayout.Label($"<color={color}>{elapsed:F0}ms</color>", _logMainStyle, GUILayout.Width(65));
            }
            else
            {
                GUILayout.Space(69); // 占位
            }

            EditorGUILayout.EndHorizontal();

            // 4. 错误详情行
            if (!log.IsSuccess)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(153); // 对齐协议名起始位置 (85+18+45+5间距)
                    string errorMsg = !string.IsNullOrEmpty(log.ValidMsg) ? log.ValidMsg : $"Status: {log.ResponseStatus}";
                    GUILayout.Label($"└─ <color=#f44747>[ERR] {errorMsg}</color>", _logErrorStyle);
                }
            }
        }

        // --- 其他 UI 辅助方法 ---

        private void DrawToolbar(GameClient client, NetworkStatistics stats)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var state = client.State;
                GUILayout.Label($"● {GetStateText(state)}", GetStateStyle(state), GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledGroupScope(state == ConnectionState.Connecting || state == ConnectionState.Reconnecting))
                {
                    if (client.IsConnected)
                    {
                        if (GUILayout.Button("断开", EditorStyles.toolbarButton, GUILayout.Width(60))) client.Disconnect();
                    }
                    else if (state == ConnectionState.Disconnected)
                    {
                        if (GUILayout.Button("连接", EditorStyles.toolbarButton, GUILayout.Width(60))) client.Connect();
                    }
                }
                if (GUILayout.Button("重置统计", EditorStyles.toolbarButton, GUILayout.Width(70))) stats.Reset();
            }
        }

        private void DrawConnectionSection(GameClient client, GameClientOptions options)
        {
            _connectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_connectionFoldout, "连接状态");
            if (_connectionFoldout)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    DrawLabelValue("状态:", GetStateText(client.State), GetStateStyle(client.State));
                    DrawLabelValue("服务器:", $"{options.Host}:{options.Port}");
                    if (PiscesSettings.Instance != null) DrawLabelValue("环境:", PiscesSettings.Instance.ActiveEnvironment.Name);
                    if (client.Statistics.ConnectedTime.HasValue) DrawLabelValue("连接时长:", NetworkStatistics.FormatTimeSpan(client.Statistics.ConnectionDuration));
                    DrawLabelValue("协议:", options.ChannelType.ToString());
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStatisticsSection(NetworkStatistics stats)
        {
            _statisticsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_statisticsFoldout, "网络统计");
            if (_statisticsFoldout)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    DrawLabelValue("RTT:", $"{TimeUtils.RttMs:F0} ms");
                    EditorGUILayout.Space(2);
                    DrawLabelValue("↑ 发送:", $"{stats.TotalSendCount:N0} 条 ({NetworkStatistics.FormatBytes(stats.TotalSendBytes)})", null, NetworkStatistics.FormatBytesPerSec(stats.SendBytesPerSec));
                    DrawLabelValue("↓ 接收:", $"{stats.TotalRecvCount:N0} 条 ({NetworkStatistics.FormatBytes(stats.TotalRecvBytes)})", null, NetworkStatistics.FormatBytesPerSec(stats.RecvBytesPerSec));
                    EditorGUILayout.Space(2);
                    DrawLabelValue("消息速率:", $"↑ {stats.SendCountPerSec:F1}/s   ↓ {stats.RecvCountPerSec:F1}/s");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawHeartbeatSection(NetworkStatistics stats, GameClientOptions options)
        {
            _heartbeatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_heartbeatFoldout, "心跳 & 重连");
            if (_heartbeatFoldout)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    DrawLabelValue("心跳间隔:", $"{options.HeartbeatIntervalSec} 秒");
                    DrawLabelValue("上次心跳:", stats.LastHeartbeatRecvTime != default ? NetworkStatistics.FormatTimeAgo(stats.LastHeartbeatRecvTime) : "-");

                    var timeoutColor = stats.CurrentHeartbeatTimeoutCount > 0 ? Color.yellow : Color.white;
                    DrawLabelValue("超时计数:", $"{stats.CurrentHeartbeatTimeoutCount} / {options.HeartbeatTimeoutCount}", new GUIStyle(EditorStyles.label) { normal = { textColor = timeoutColor } });

                    EditorGUILayout.Space(2);
                    DrawLabelValue("自动重连:", options.AutoReconnect ? "是" : "否");
                    var maxText = options.MaxReconnectCount == 0 ? "∞" : options.MaxReconnectCount.ToString();
                    DrawLabelValue("重连次数:", $"{stats.ReconnectCount} / {maxText}");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTimeSyncSection()
        {
            _timeSyncFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_timeSyncFoldout, "时间同步");
            if (_timeSyncFoldout)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    DrawLabelValue("同步状态:", TimeUtils.IsSynced ? "✓ 已同步" : "✗ 未同步");
                    if (TimeUtils.IsSynced)
                    {
                        DrawLabelValue("服务器时间:", TimeUtils.ServerTimeString);
                        DrawLabelValue("RTT:", $"{TimeUtils.RttMs:F0} ms");
                    }
                    EditorGUILayout.Space(5);
                    if (GUILayout.Button("手动同步", GUILayout.Width(80))) PiscesSdk.Instance.RequestTimeSync();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLabelValue(string label, string value, GUIStyle valueStyle = null, string rightValue = null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(80));
                GUILayout.Label(value, valueStyle ?? EditorStyles.label);
                if (!string.IsNullOrEmpty(rightValue))
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(rightValue, _logInfoStyle, GUILayout.Width(100));
                }
            }
        }

        private void DrawNotPlayingUI()
        {
            EditorGUILayout.Space(50);
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("Pisces 网络监控", _headerStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.HelpBox("请在运行模式 (Play Mode) 下查看实时网络数据", MessageType.Info);
                if (GUILayout.Button("打开项目设置", GUILayout.Width(120))) SettingsService.OpenProjectSettings("Project/Pisces Client");
            }
        }

        private void DrawNotInitializedUI()
        {
            EditorGUILayout.Space(50);
            EditorGUILayout.HelpBox("Pisces SDK 未初始化\n请确保在代码中调用了 PiscesSdk.Instance.Initialize()", MessageType.Warning);
        }

        private string GetStateText(ConnectionState state) => state switch
        {
            ConnectionState.Disconnected => "已断开",
            ConnectionState.Connecting => "正在连接",
            ConnectionState.Connected => "已连接",
            ConnectionState.Reconnecting => "尝试重连",
            ConnectionState.Closed => "已关闭",
            _ => state.ToString()
        };

        private GUIStyle GetStateStyle(ConnectionState state) => state switch
        {
            ConnectionState.Connected => _statusConnectedStyle,
            ConnectionState.Reconnecting or ConnectionState.Connecting => _statusReconnectingStyle,
            _ => _statusDisconnectedStyle
        };
    }
}
