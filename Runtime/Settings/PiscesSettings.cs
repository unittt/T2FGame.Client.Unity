using System;
using System.Collections.Generic;
using Pisces.Client.Network.Channel;
using Pisces.Client.Network;
using Pisces.Client.Utils;
using UnityEngine;

namespace Pisces.Client.Settings
{
    /// <summary>
    /// 服务器环境配置
    /// </summary>
    [Serializable]
    public class ServerEnvironment
    {
        /// <summary>
        /// 环境名称（如 Development, Staging, Production）
        /// </summary>
        public string Name = "Development";

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host = "localhost";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port = 9090;

        /// <summary>
        /// 环境描述（可选）
        /// </summary>
        [TextArea(1, 3)]
        public string Description;

        public ServerEnvironment() { }

        public ServerEnvironment(string name, string host, int port, string description = "")
        {
            Name = name;
            Host = host;
            Port = port;
            Description = description;
        }
    }

    /// <summary>
    /// Pisces Client SDK 全局配置
    /// 通过 Project Settings 进行配置
    /// </summary>
    public class PiscesSettings : ScriptableObject
    {
        
        private static PiscesSettings _instance;

        /// <summary>
        /// 获取全局配置实例
        /// </summary>
        public static PiscesSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreateInstance();
                }
                return _instance;
            }
        }

        private static PiscesSettings LoadOrCreateInstance()
        {
            var settings = Resources.Load<PiscesSettings>(SettingsPaths.PiscesSettingsResourcePath);

            // 如果在编辑器中未找到，自动创建
            if (settings == null)
            {
                settings = CreateInstance<PiscesSettings>();
                settings.ResetToDefaults();
#if UNITY_EDITOR
                // 保存到 Resources 文件夹
                UnityEditor.AssetDatabase.CreateAsset(settings, SettingsPaths.PiscesSettingsAssetPath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
            return settings;
        }


        #region Server Environment

        [Header("Server Environment")]
        [Tooltip("服务器环境列表")]
        [SerializeField]
        private List<ServerEnvironment> _serverEnvironments = new()
        {
            new ServerEnvironment("Development", "localhost", 9090, "本地开发环境"),
            new ServerEnvironment("Staging", "staging.example.com", 9090, "测试环境"),
            new ServerEnvironment("Production", "prod.example.com", 9090, "生产环境")
        };

        [Tooltip("当前激活的服务器环境索引")]
        [SerializeField]
        private int _activeEnvironmentIndex;

        /// <summary>
        /// 服务器环境列表
        /// </summary>
        public List<ServerEnvironment> ServerEnvironments => _serverEnvironments;

        /// <summary>
        /// 当前激活的服务器环境索引
        /// </summary>
        public int ActiveEnvironmentIndex
        {
            get => _activeEnvironmentIndex;
            set => _activeEnvironmentIndex = Mathf.Clamp(value, 0, Mathf.Max(0, _serverEnvironments.Count - 1));
        }

        /// <summary>
        /// 获取当前激活的服务器环境
        /// </summary>
        public ServerEnvironment ActiveEnvironment
        {
            get
            {
                if (_serverEnvironments == null || _serverEnvironments.Count == 0)
                    return new ServerEnvironment();

                var index = Mathf.Clamp(_activeEnvironmentIndex, 0, _serverEnvironments.Count - 1);
                return _serverEnvironments[index];
            }
        }

        #endregion

        #region Network Settings

        [Header("Network Settings")]
        [Tooltip("传输协议类型")]
        [SerializeField]
        private ChannelType _channelType = ChannelType.Tcp;

        [Tooltip("连接超时时间（毫秒）")]
        [SerializeField]
        [Range(1000, 60000)]
        private int _connectTimeoutMs = 10000;

        [Tooltip("请求超时时间（毫秒）")]
        [SerializeField]
        [Range(1000, 120000)]
        private int _requestTimeoutMs = 30000;

        /// <summary>
        /// 传输协议类型
        /// </summary>
        public ChannelType ChannelType
        {
            get => _channelType;
            set => _channelType = value;
        }

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeoutMs
        {
            get => _connectTimeoutMs;
            set => _connectTimeoutMs = Mathf.Clamp(value, 1000, 60000);
        }

        /// <summary>
        /// 请求超时时间（毫秒）
        /// </summary>
        public int RequestTimeoutMs
        {
            get => _requestTimeoutMs;
            set => _requestTimeoutMs = Mathf.Clamp(value, 1000, 120000);
        }

        #endregion

        #region Heartbeat Settings

        [Header("Heartbeat Settings")]
        [Tooltip("心跳间隔（秒）")]
        [SerializeField]
        [Range(5, 120)]
        private int _heartbeatIntervalSec = 30;

        [Tooltip("心跳超时次数（超过此次数认为连接断开）")]
        [SerializeField]
        [Range(1, 10)]
        private int _heartbeatTimeoutCount = 3;

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public int HeartbeatIntervalSec
        {
            get => _heartbeatIntervalSec;
            set => _heartbeatIntervalSec = Mathf.Clamp(value, 5, 120);
        }

        /// <summary>
        /// 心跳超时次数
        /// </summary>
        public int HeartbeatTimeoutCount
        {
            get => _heartbeatTimeoutCount;
            set => _heartbeatTimeoutCount = Mathf.Clamp(value, 1, 10);
        }

        #endregion

        #region Reconnect Settings

        [Header("Reconnect Settings")]
        [Tooltip("是否自动重连")]
        [SerializeField]
        private bool _autoReconnect = true;

        [Tooltip("重连间隔（秒）")]
        [SerializeField]
        [Range(1, 30)]
        private int _reconnectIntervalSec = 3;

        [Tooltip("最大重连次数（0 = 无限重试）")]
        [SerializeField]
        [Range(0, 100)]
        private int _maxReconnectCount = 5;

        /// <summary>
        /// 是否自动重连
        /// </summary>
        public bool AutoReconnect
        {
            get => _autoReconnect;
            set => _autoReconnect = value;
        }

        /// <summary>
        /// 重连间隔（秒）
        /// </summary>
        public int ReconnectIntervalSec
        {
            get => _reconnectIntervalSec;
            set => _reconnectIntervalSec = Mathf.Clamp(value, 1, 30);
        }

        /// <summary>
        /// 最大重连次数
        /// </summary>
        public int MaxReconnectCount
        {
            get => _maxReconnectCount;
            set => _maxReconnectCount = Mathf.Max(0, value);
        }

        #endregion

        #region Buffer Settings

        [Header("Buffer Settings")]
        [Tooltip("接收缓冲区大小（字节）")]
        [SerializeField]
        private int _receiveBufferSize = 65536;

        [Tooltip("发送缓冲区大小（字节）")]
        [SerializeField]
        private int _sendBufferSize = 65536;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        public int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set => _receiveBufferSize = Mathf.Max(1024, value);
        }

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        public int SendBufferSize
        {
            get => _sendBufferSize;
            set => _sendBufferSize = Mathf.Max(1024, value);
        }

        #endregion

        #region Rate Limit Settings

        [Header("Rate Limit Settings")]
        [Tooltip("是否启用发送限流")]
        [SerializeField]
        private bool _enableRateLimit = true;

        [Tooltip("每秒最大发送消息数")]
        [SerializeField]
        [Range(10, 1000)]
        private int _maxSendRate = 100;

        [Tooltip("最大突发消息数（桶容量）")]
        [SerializeField]
        [Range(10, 200)]
        private int _maxBurstSize = 50;

        /// <summary>
        /// 是否启用发送限流
        /// </summary>
        public bool EnableRateLimit
        {
            get => _enableRateLimit;
            set => _enableRateLimit = value;
        }

        /// <summary>
        /// 每秒最大发送消息数
        /// </summary>
        public int MaxSendRate
        {
            get => _maxSendRate;
            set => _maxSendRate = Mathf.Clamp(value, 10, 1000);
        }

        /// <summary>
        /// 最大突发消息数
        /// </summary>
        public int MaxBurstSize
        {
            get => _maxBurstSize;
            set => _maxBurstSize = Mathf.Clamp(value, 10, 200);
        }

        #endregion

        #region Debug Settings

        [Header("Debug Settings")]
        [Tooltip("日志级别")]
        [SerializeField]
        private GameLogLevel _logLevel = GameLogLevel.Info;

        [Tooltip("是否使用工作线程进行网络收发（WebGL/微信小游戏不支持）")]
        [SerializeField]
        private bool _useWorkerThread = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        public GameLogLevel LogLevel
        {
            get => _logLevel;
            set => _logLevel = value;
        }

        /// <summary>
        /// 是否使用工作线程
        /// </summary>
        public bool UseWorkerThread
        {
            get => _useWorkerThread;
            set => _useWorkerThread = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 将配置转换为 GameClientOptions
        /// </summary>
        /// <returns>GameClientOptions 实例</returns>
        public GameClientOptions ToGameClientOptions()
        {
            var env = ActiveEnvironment;

            return new GameClientOptions
            {
                ChannelType = _channelType,
                Host = env.Host,
                Port = env.Port,
                ConnectTimeoutMs = _connectTimeoutMs,
                RequestTimeoutMs = _requestTimeoutMs,
                HeartbeatIntervalSec = _heartbeatIntervalSec,
                HeartbeatTimeoutCount = _heartbeatTimeoutCount,
                AutoReconnect = _autoReconnect,
                ReconnectIntervalSec = _reconnectIntervalSec,
                MaxReconnectCount = _maxReconnectCount,
                ReceiveBufferSize = _receiveBufferSize,
                SendBufferSize = _sendBufferSize,
                EnableRateLimit = _enableRateLimit,
                MaxSendRate = _maxSendRate,
                MaxBurstSize = _maxBurstSize,
                LogLevel = _logLevel,
                UseWorkerThread = GetEffectiveUseWorkerThread()
            };
        }

        /// <summary>
        /// 获取实际的 UseWorkerThread 值（考虑平台限制）
        /// </summary>
        private bool GetEffectiveUseWorkerThread()
        {
#if UNITY_WEBGL || UNITY_WEIXINMINIGAME
            return false; // 这些平台不支持多线程
#else
            return _useWorkerThread;
#endif
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            _serverEnvironments = new List<ServerEnvironment>
            {
                new("Development", "localhost", 9090, "本地开发环境"),
                new("Staging", "staging.example.com", 9090, "测试环境"),
                new("Production", "prod.example.com", 9090, "生产环境")
            };
            _activeEnvironmentIndex = 0;

            _channelType = ChannelType.Tcp;
            _connectTimeoutMs = 10000;
            _requestTimeoutMs = 30000;

            _heartbeatIntervalSec = 30;
            _heartbeatTimeoutCount = 3;

            _autoReconnect = true;
            _reconnectIntervalSec = 3;
            _maxReconnectCount = 5;

            _receiveBufferSize = 65536;
            _sendBufferSize = 65536;

            _enableRateLimit = true;
            _maxSendRate = 100;
            _maxBurstSize = 50;

            _logLevel = GameLogLevel.Info;
            _useWorkerThread = true;
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <param name="errors">错误信息列表</param>
        /// <returns>是否有效</returns>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (_serverEnvironments == null || _serverEnvironments.Count == 0)
            {
                errors.Add("至少需要配置一个服务器环境");
            }
            else
            {
                for (int i = 0; i < _serverEnvironments.Count; i++)
                {
                    var env = _serverEnvironments[i];
                    if (string.IsNullOrWhiteSpace(env.Name))
                        errors.Add($"服务器环境 [{i}] 名称不能为空");
                    if (string.IsNullOrWhiteSpace(env.Host))
                        errors.Add($"服务器环境 [{i}] ({env.Name}) 地址不能为空");
                    if (env.Port <= 0 || env.Port > 65535)
                        errors.Add($"服务器环境 [{i}] ({env.Name}) 端口必须在 1-65535 之间");
                }
            }

            if (_connectTimeoutMs < 1000)
                errors.Add("连接超时时间不能小于 1000ms");

            if (_requestTimeoutMs < 1000)
                errors.Add("请求超时时间不能小于 1000ms");

            if (_heartbeatIntervalSec < 5)
                errors.Add("心跳间隔不能小于 5 秒");

            if (_receiveBufferSize < 1024)
                errors.Add("接收缓冲区不能小于 1024 字节");

            if (_sendBufferSize < 1024)
                errors.Add("发送缓冲区不能小于 1024 字节");

            return errors.Count == 0;
        }

        #endregion
    }
}
