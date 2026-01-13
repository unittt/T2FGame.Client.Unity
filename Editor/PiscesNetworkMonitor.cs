using System.Collections.Generic;
using Pisces.Client.Network;
using Pisces.Client.Network.Core;
using Pisces.Client.Settings;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Pisces.Client.Editor
{
    /// <summary>
    /// Pisces 网络监控窗口
    /// 运行时显示网络状态、统计信息和消息日志
    /// </summary>
    public class PiscesNetworkMonitor : EditorWindow
    {
        // 窗口状态
        private Vector2 _mainScrollPosition;
        private Vector2 _logScrollPosition;
        private string _logFilter = "";
        private bool _autoScroll = true;
        private int _logTypeFilter; // 0=全部, 1=发送, 2=接收

        // 刷新控制
        private double _lastRepaintTime;
        private const double RepaintInterval = 0.1; // 100ms 刷新一次

        // 缓存的日志
        private List<NetworkMessageLog> _cachedLogs = new();
        private int _lastLogCount;

        // 样式
        private GUIStyle _statusConnectedStyle;
        private GUIStyle _statusDisconnectedStyle;
        private GUIStyle _statusReconnectingStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _logEntryStyle;
        private GUIStyle _logSendStyle;
        private GUIStyle _logRecvStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized;

        // Foldout 状态
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
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 仅在运行时刷新
            if (!Application.isPlaying) return;

            // 限制刷新频率
            if (EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)
            {
                _lastRepaintTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _statusConnectedStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                fontSize = 14
            };

            _statusDisconnectedStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.8f, 0.3f, 0.3f) },
                fontSize = 14
            };

            _statusReconnectingStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 0.2f) },
                fontSize = 14
            };

            _boxStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            _logEntryStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                richText = true,
                wordWrap = false
            };

            _logSendStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = new Color(0.6f, 0.8f, 1f) }
            };

            _logRecvStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = new Color(0.6f, 1f, 0.6f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            // 非运行时显示提示
            if (!Application.isPlaying)
            {
                DrawNotPlayingUI();
                return;
            }

            // 检查 SDK 是否初始化
            if (!PiscesSdk.Instance.IsInitialized)
            {
                DrawNotInitializedUI();
                return;
            }

            var client = PiscesSdk.Instance.Client;
            if (client == null)
            {
                EditorGUILayout.HelpBox("客户端实例为空", MessageType.Warning);
                return;
            }

            var stats = client.Statistics;
            var options = client.Options;

            // 更新速率统计
            stats.UpdateRates();

            // 工具栏
            DrawToolbar(client, stats);

            EditorGUILayout.Space(5);

            // 主内容区域
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_mainScrollPosition))
            {
                _mainScrollPosition = scrollView.scrollPosition;

                DrawConnectionSection(client, options);
                DrawStatisticsSection(stats);
                DrawHeartbeatSection(stats, options);
                DrawTimeSyncSection();
                DrawLogSection(stats);
            }
        }

        private void DrawNotPlayingUI()
        {
            EditorGUILayout.Space(50);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("网络监控", _headerStyle ?? EditorStyles.boldLabel);
                    EditorGUILayout.Space(10);
                    EditorGUILayout.HelpBox("请在运行模式下使用此窗口", MessageType.Info);

                    EditorGUILayout.Space(10);
                    if (GUILayout.Button("打开设置", GUILayout.Width(100)))
                    {
                        SettingsService.OpenProjectSettings("Project/Pisces Client");
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawNotInitializedUI()
        {
            EditorGUILayout.Space(50);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.HelpBox("Pisces SDK 未初始化\n\n请在代码中调用:\nPiscesSdk.Instance.Initialize();", UnityEditor.MessageType.Warning);
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawToolbar(GameClient client, NetworkStatistics stats)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 连接状态指示
                var state = client.State;
                var stateText = GetStateText(state);
                var stateStyle = GetStateStyle(state);
                GUILayout.Label($"● {stateText}", stateStyle, GUILayout.Width(100));

                GUILayout.FlexibleSpace();

                // 操作按钮
                using (new EditorGUI.DisabledGroupScope(state == ConnectionState.Connecting || state == ConnectionState.Reconnecting))
                {
                    if (client.IsConnected)
                    {
                        if (GUILayout.Button("断开", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        {
                            client.Disconnect();
                        }
                    }
                    else if (state == ConnectionState.Disconnected)
                    {
                        if (GUILayout.Button("连接", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        {
                            client.Connect();
                        }
                    }
                }

                if (GUILayout.Button("重置统计", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    stats.Reset();
                }
            }
        }

        private void DrawConnectionSection(GameClient client, GameClientOptions options)
        {
            _connectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_connectionFoldout, "连接状态");
            if (_connectionFoldout)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    var state = client.State;
                    var stateStyle = GetStateStyle(state);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("状态:", GUILayout.Width(60));
                        GUILayout.Label($"● {GetStateText(state)}", stateStyle);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("服务器:", GUILayout.Width(60));
                        GUILayout.Label($"{options.Host}:{options.Port}");
                    }

                    // 当前环境
                    var settings = PiscesSettings.Instance;
                    if (settings != null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("环境:", GUILayout.Width(60));
                            GUILayout.Label(settings.ActiveEnvironment.Name);
                        }
                    }

                    // 连接时长
                    var stats = client.Statistics;
                    if (stats.ConnectedTime.HasValue)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("连接时长:", GUILayout.Width(60));
                            GUILayout.Label(NetworkStatistics.FormatTimeSpan(stats.ConnectionDuration));
                        }
                    }

                    // 协议类型
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("协议:", GUILayout.Width(60));
                        GUILayout.Label(options.ChannelType.ToString());
                    }
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
                    // RTT
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("RTT:", GUILayout.Width(80));
                        GUILayout.Label($"{TimeUtils.RttMs:F0} ms");
                    }

                    EditorGUILayout.Space(5);

                    // 发送统计
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("↑ 发送:", GUILayout.Width(80));
                        GUILayout.Label($"{stats.TotalSendCount:N0} 条  ({NetworkStatistics.FormatBytes(stats.TotalSendBytes)})");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{NetworkStatistics.FormatBytesPerSec(stats.SendBytesPerSec)}", GUILayout.Width(100));
                    }

                    // 接收统计
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("↓ 接收:", GUILayout.Width(80));
                        GUILayout.Label($"{stats.TotalRecvCount:N0} 条  ({NetworkStatistics.FormatBytes(stats.TotalRecvBytes)})");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{NetworkStatistics.FormatBytesPerSec(stats.RecvBytesPerSec)}", GUILayout.Width(100));
                    }

                    EditorGUILayout.Space(5);

                    // 消息速率
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("消息速率:", GUILayout.Width(80));
                        GUILayout.Label($"↑ {stats.SendCountPerSec:F1}/s   ↓ {stats.RecvCountPerSec:F1}/s");
                    }
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
                    // 心跳状态
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("心跳间隔:", GUILayout.Width(80));
                        GUILayout.Label($"{options.HeartbeatIntervalSec} 秒");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("上次心跳:", GUILayout.Width(80));
                        if (stats.LastHeartbeatRecvTime != default)
                        {
                            GUILayout.Label(NetworkStatistics.FormatTimeAgo(stats.LastHeartbeatRecvTime));
                        }
                        else
                        {
                            GUILayout.Label("-");
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("超时计数:", GUILayout.Width(80));
                        var timeoutColor = stats.CurrentHeartbeatTimeoutCount > 0 ? Color.yellow : Color.white;
                        var prevColor = GUI.color;
                        GUI.color = timeoutColor;
                        GUILayout.Label($"{stats.CurrentHeartbeatTimeoutCount} / {options.HeartbeatTimeoutCount}");
                        GUI.color = prevColor;
                    }

                    EditorGUILayout.Space(5);

                    // 重连状态
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("自动重连:", GUILayout.Width(80));
                        GUILayout.Label(options.AutoReconnect ? "是" : "否");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("重连次数:", GUILayout.Width(80));
                        var maxText = options.MaxReconnectCount == 0 ? "∞" : options.MaxReconnectCount.ToString();
                        GUILayout.Label($"{stats.ReconnectCount} / {maxText}");
                    }
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
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("同步状态:", GUILayout.Width(80));
                        GUILayout.Label(TimeUtils.IsSynced ? "✓ 已同步" : "✗ 未同步");
                    }

                    if (TimeUtils.IsSynced)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("服务器时间:", GUILayout.Width(80));
                            GUILayout.Label(TimeUtils.ServerTimeString);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("RTT:", GUILayout.Width(80));
                            GUILayout.Label($"{TimeUtils.RttMs:F0} ms");
                        }
                    }

                    EditorGUILayout.Space(5);

                    if (GUILayout.Button("请求同步", GUILayout.Width(80)))
                    {
                        PiscesSdk.Instance.RequestTimeSync();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLogSection(NetworkStatistics stats)
        {
            _logFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_logFoldout, $"消息日志 ({stats.LogCount})");
            if (_logFoldout)
            {
                // 日志工具栏
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 过滤器
                    GUILayout.Label("过滤:", GUILayout.Width(35));
                    _logTypeFilter = EditorGUILayout.Popup(_logTypeFilter, new[] { "全部", "发送 ↑", "接收 ↓" }, GUILayout.Width(80));

                    _logFilter = EditorGUILayout.TextField(_logFilter, EditorStyles.toolbarSearchField);

                    _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", GUILayout.Width(70));

                    if (GUILayout.Button("清除", GUILayout.Width(50)))
                    {
                        stats.ClearLogs();
                        _cachedLogs.Clear();
                    }
                }

                EditorGUILayout.Space(3);

                // 刷新日志缓存
                if (stats.LogCount != _lastLogCount)
                {
                    _cachedLogs = stats.GetLogs(100);
                    _lastLogCount = stats.LogCount;
                }

                // 日志列表
                var logAreaHeight = Mathf.Max(150, position.height - 450);
                using (var scrollView = new EditorGUILayout.ScrollViewScope(_logScrollPosition, GUILayout.Height(logAreaHeight)))
                {
                    _logScrollPosition = scrollView.scrollPosition;

                    foreach (var log in _cachedLogs)
                    {
                        switch (_logTypeFilter)
                        {
                            // 应用过滤
                            case 1 when !log.IsOutgoing:
                            case 2 when log.IsOutgoing:
                                continue;
                        }

                        if (!string.IsNullOrEmpty(_logFilter))
                        {
                            if (!log.CmdDisplay.Contains(_logFilter))
                            {
                                continue;
                            }
                        }

                        DrawLogEntry(log);
                    }

                    // 自动滚动
                    if (_autoScroll && Event.current.type == EventType.Repaint)
                    {
                        _logScrollPosition = new Vector2(0, 0);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLogEntry(NetworkMessageLog log)
        {
            var style = log.IsOutgoing ? _logSendStyle : _logRecvStyle;
            var arrow = log.IsOutgoing ? "↑" : "↓";
            var timeStr = log.Timestamp.ToString("HH:mm:ss.fff");

            // 命令显示
            var cmdStr = log.CmdDisplay;

            // MsgId 显示
            var msgIdStr = log.MsgId != 0 ? $" #{log.MsgId}" : "";

            // 广播标记
            var broadcastStr = log.IsBroadcast ? " <color=cyan>[广播]</color>" : "";

            // 数据大小
            var sizeStr = log.DataSize > 0 ? $" ({log.DataSize}B)" : "";

            // 响应耗时显示
            var elapsedStr = "";
            if (!log.IsOutgoing && log.ElapsedMs.HasValue)
            {
                var elapsed = log.ElapsedMs.Value;
                var color = elapsed < 100 ? "lime" : elapsed < 500 ? "yellow" : "red";
                elapsedStr = $" <color={color}>[{elapsed:F0}ms]</color>";
            }

            // 错误状态显示
            var errorStr = "";
            if (!log.IsSuccess)
            {
                var errorMsg = !string.IsNullOrEmpty(log.ValidMsg) ? log.ValidMsg : $"Status:{log.ResponseStatus}";
                errorStr = $" <color=red>[ERR:{log.ResponseStatus}] {errorMsg}</color>";
            }

            var text = $"{timeStr} {arrow} {cmdStr}{msgIdStr}{sizeStr}{broadcastStr}{elapsedStr}{errorStr}";

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(text, style);
            }
        }

        private string GetStateText(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Disconnected => "已断开",
                ConnectionState.Connecting => "连接中...",
                ConnectionState.Connected => "已连接",
                ConnectionState.Reconnecting => "重连中...",
                ConnectionState.Closed => "已关闭",
                _ => state.ToString()
            };
        }

        private GUIStyle GetStateStyle(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => _statusConnectedStyle,
                ConnectionState.Reconnecting or ConnectionState.Connecting => _statusReconnectingStyle,
                _ => _statusDisconnectedStyle
            };
        }
    }
}
