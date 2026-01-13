using System;
using Pisces.Client.Sdk;
using Pisces.Protocol;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace Pisces.Client.Network.Core
{
#if UNITY_EDITOR
    /// <summary>
    /// 网络消息日志条目（仅编辑器，仅业务消息）
    /// </summary>
    internal class NetworkMessageLog
    {
        public DateTime Timestamp { get; set; }
        public bool IsOutgoing { get; set; }
        public int CmdMerge { get; set; }
        public int MsgId { get; set; }
        public int DataSize { get; set; }
        public bool IsSuccess { get; set; } = true;
        public int ResponseStatus { get; set; }
        public string ValidMsg { get; set; }
        public float? ElapsedMs { get; set; }

        /// <summary>
        /// 是否为广播消息 (接收消息且无MsgId)
        /// </summary>
        public bool IsBroadcast => !IsOutgoing && MsgId == 0;

        /// <summary>
        /// 获取格式化的 Cmd 显示
        /// </summary>
        public string CmdDisplay => CmdKit.ToString(CmdMerge);
    }
#endif

    /// <summary>
    /// 网络统计数据
    /// 生产环境仅保留基础计数器（零GC）
    /// 编辑器环境提供完整的消息日志功能
    /// </summary>
    internal class NetworkStatistics
    {
        // shared constants
        private const float RateUpdateInterval = 1f;

#if UNITY_EDITOR
        private const int MaxLogCount = 200;
        private const int MaxPendingRequests = 1000;
#endif

        #region 生产环境字段（零GC）

        // 基础统计
        private long _totalSendCount;
        private long _totalRecvCount;
        private long _totalSendBytes;
        private long _totalRecvBytes;

        // 速率计算
        private long _lastSendBytes;
        private long _lastRecvBytes;
        private long _lastSendCount;
        private long _lastRecvCount;
        private float _lastRateUpdateTime;
        private float _sendBytesPerSec;
        private float _recvBytesPerSec;
        private float _sendCountPerSec;
        private float _recvCountPerSec;

        // 心跳统计
        private DateTime _lastHeartbeatSendTime;
        private DateTime _lastHeartbeatRecvTime;
        private int _currentHeartbeatTimeoutCount;

        // 重连统计
        private int _reconnectCount;
        private DateTime? _connectedTime;

        // 限流统计
        private long _rateLimitedCount;

        // 发送失败统计
        private long _sendFailedCount;

        #endregion

#if UNITY_EDITOR
        #region 编辑器专用字段（有GC开销）

        private readonly LinkedList<NetworkMessageLog> _messageLogs = new();
        private readonly object _logLock = new();
        private readonly Dictionary<int, DateTime> _pendingRequestTimes = new();
        private bool _enableLogging = true;

        #endregion
#endif

        #region 生产环境属性

        public long TotalSendCount => _totalSendCount;
        public long TotalRecvCount => _totalRecvCount;
        public long TotalSendBytes => _totalSendBytes;
        public long TotalRecvBytes => _totalRecvBytes;
        public float SendBytesPerSec => _sendBytesPerSec;
        public float RecvBytesPerSec => _recvBytesPerSec;
        public float SendCountPerSec => _sendCountPerSec;
        public float RecvCountPerSec => _recvCountPerSec;
        public DateTime LastHeartbeatSendTime => _lastHeartbeatSendTime;
        public DateTime LastHeartbeatRecvTime => _lastHeartbeatRecvTime;
        public int CurrentHeartbeatTimeoutCount => _currentHeartbeatTimeoutCount;
        public int ReconnectCount => _reconnectCount;
        public long RateLimitedCount => _rateLimitedCount;
        public long SendFailedCount => _sendFailedCount;
        public DateTime? ConnectedTime => _connectedTime;

        public TimeSpan ConnectionDuration =>
            _connectedTime.HasValue ? DateTime.Now - _connectedTime.Value : TimeSpan.Zero;

        #endregion

#if UNITY_EDITOR
        #region 编辑器专用属性

        public int LogCount
        {
            get
            {
                lock (_logLock)
                {
                    return _messageLogs.Count;
                }
            }
        }

        public bool EnableLogging
        {
            get => _enableLogging;
            set => _enableLogging = value;
        }

        #endregion
#endif

        #region 记录方法

        /// <summary>
        /// 记录发送消息
        /// </summary>
        public void RecordSend(int bytes, RequestCommand command)
        {
            _totalSendCount++;
            _totalSendBytes += bytes;

#if UNITY_EDITOR
            // 只记录业务消息
            if (command.MessageType != MessageType.Business)
                return;

            if (command.MsgId != 0)
            {
                lock (_logLock)
                {
                    if (_pendingRequestTimes.Count >= MaxPendingRequests)
                    {
                        _pendingRequestTimes.Clear();
                    }
                    _pendingRequestTimes[command.MsgId] = DateTime.Now;
                }
            }

            if (_enableLogging)
            {
                AddLog(new NetworkMessageLog
                {
                    Timestamp = DateTime.Now,
                    IsOutgoing = true,
                    CmdMerge = command.CmdMerge,
                    MsgId = command.MsgId,
                    DataSize = bytes,
                    IsSuccess = true
                });
            }
#endif
        }

        /// <summary>
        /// 记录接收消息
        /// </summary>
        public void RecordReceive(ExternalMessage message)
        {
            var dataSize = message.Data?.Length ?? 0;
            _totalRecvCount++;
            _totalRecvBytes += dataSize;

#if UNITY_EDITOR
            // 只记录业务消息
            if (!_enableLogging || message.MessageType != MessageType.Business || message.CmdMerge == 0)
                return;

            float? elapsedMs = null;
            if (message.MsgId != 0)
            {
                lock (_logLock)
                {
                    if (_pendingRequestTimes.TryGetValue(message.MsgId, out var sendTime))
                    {
                        elapsedMs = (float)(DateTime.Now - sendTime).TotalMilliseconds;
                        _pendingRequestTimes.Remove(message.MsgId);
                    }
                }
            }

            AddLog(new NetworkMessageLog
            {
                Timestamp = DateTime.Now,
                IsOutgoing = false,
                CmdMerge = message.CmdMerge,
                MsgId = message.MsgId,
                DataSize = dataSize,
                IsSuccess = message.ResponseStatus == 0,
                ResponseStatus = message.ResponseStatus,
                ValidMsg = string.IsNullOrEmpty(message.ValidMsg) ? null : message.ValidMsg,
                ElapsedMs = elapsedMs
            });
#endif
        }

        public void RecordHeartbeatSend()
        {
            _lastHeartbeatSendTime = DateTime.Now;
            _currentHeartbeatTimeoutCount++;
        }

        public void RecordHeartbeatReceive()
        {
            _lastHeartbeatRecvTime = DateTime.Now;
            _currentHeartbeatTimeoutCount = 0;
        }

        public void RecordConnected()
        {
            _connectedTime = DateTime.Now;
        }

        public void RecordDisconnected()
        {
            _connectedTime = null;
        }

        public void RecordReconnect()
        {
            _reconnectCount++;
        }

        public void RecordRateLimited()
        {
            _rateLimitedCount++;
        }

        public void RecordSendFailed()
        {
            _sendFailedCount++;
        }

        public void ResetReconnectCount()
        {
            _reconnectCount = 0;
        }

        #endregion

        #region 速率计算

        public void UpdateRates()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastRateUpdateTime;

            if (deltaTime >= RateUpdateInterval)
            {
                long sendBytesDelta = _totalSendBytes - _lastSendBytes;
                long recvBytesDelta = _totalRecvBytes - _lastRecvBytes;
                long sendCountDelta = _totalSendCount - _lastSendCount;
                long recvCountDelta = _totalRecvCount - _lastRecvCount;

                _sendBytesPerSec = sendBytesDelta / deltaTime;
                _recvBytesPerSec = recvBytesDelta / deltaTime;
                _sendCountPerSec = sendCountDelta / deltaTime;
                _recvCountPerSec = recvCountDelta / deltaTime;

                _lastSendBytes = _totalSendBytes;
                _lastRecvBytes = _totalRecvBytes;
                _lastSendCount = _totalSendCount;
                _lastRecvCount = _totalRecvCount;
                _lastRateUpdateTime = currentTime;
            }
        }

        #endregion

#if UNITY_EDITOR
        #region 日志管理（仅编辑器）

        private void AddLog(NetworkMessageLog log)
        {
            lock (_logLock)
            {
                _messageLogs.AddFirst(log);
                while (_messageLogs.Count > MaxLogCount)
                {
                    _messageLogs.RemoveLast();
                }
            }
        }

        public List<NetworkMessageLog> GetLogs(int maxCount = 100)
        {
            lock (_logLock)
            {
                var result = new List<NetworkMessageLog>();
                int count = 0;
                foreach (var log in _messageLogs)
                {
                    if (count >= maxCount) break;
                    result.Add(log);
                    count++;
                }
                return result;
            }
        }

        public void ClearLogs()
        {
            lock (_logLock)
            {
                _messageLogs.Clear();
            }
        }

        #endregion
#endif

        #region 重置

        public void Reset()
        {
            _totalSendCount = 0;
            _totalRecvCount = 0;
            _totalSendBytes = 0;
            _totalRecvBytes = 0;

            _lastSendBytes = 0;
            _lastRecvBytes = 0;
            _lastSendCount = 0;
            _lastRecvCount = 0;

            _sendBytesPerSec = 0;
            _recvBytesPerSec = 0;
            _sendCountPerSec = 0;
            _recvCountPerSec = 0;

            _currentHeartbeatTimeoutCount = 0;
            _reconnectCount = 0;
            _rateLimitedCount = 0;
            _sendFailedCount = 0;
            _connectedTime = null;

#if UNITY_EDITOR
            lock (_logLock)
            {
                _pendingRequestTimes.Clear();
            }
            ClearLogs();
#endif
        }

        #endregion

        #region 格式化辅助方法

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024f:F2} KB";
            return $"{bytes} B";
        }

        public static string FormatBytesPerSec(float bytesPerSec)
        {
            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024f * 1024f):F2} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024f:F2} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public static string FormatTimeAgo(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalSeconds < 1)
                return "刚刚";
            if (diff.TotalSeconds < 60)
                return $"{(int)diff.TotalSeconds}秒前";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}分钟前";
            return $"{(int)diff.TotalHours}小时前";
        }

        #endregion
    }
}
