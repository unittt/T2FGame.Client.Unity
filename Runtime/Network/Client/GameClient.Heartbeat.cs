using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using UnityEngine;

namespace Pisces.Client.Network
{
    /// <summary>
    /// GameClient - 心跳部分
    /// </summary>
    public partial class GameClient
    {
        // 心跳相关
        private CancellationTokenSource _heartbeatCts;
        private int _heartbeatTimeoutCount;
        
        
        private void StartHeartbeat()
        {
            StopHeartbeat();

            _heartbeatCts = new CancellationTokenSource();
            _heartbeatTimeoutCount = 0;

            HeartbeatLoop(_heartbeatCts.Token).Forget();
        }

        private void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        private async UniTaskVoid HeartbeatLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected && Application.isPlaying)
            {
                try
                {
                    // 先等待一个心跳周期
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_options.HeartbeatIntervalSec),
                        cancellationToken: cancellationToken
                    );

                    // Unity 退出或连接断开时停止
                    if (!Application.isPlaying || !IsConnected)
                        break;

                    // 先递增计数（表示即将发送一个待确认的心跳）
                    _heartbeatTimeoutCount++;

                    // 检查是否超时（在发送前检查）
                    // 如果已经有 N 个心跳未收到响应，说明连接已失效
                    if (_heartbeatTimeoutCount > _options.HeartbeatTimeoutCount)
                    {
                        GameLogger.LogWarning(
                            $"[GameClient] 心跳超时: 连续 {_heartbeatTimeoutCount - 1} 次未收到响应"
                        );
                        _channel?.Disconnect();
                        break;
                    }

                    // 发送心跳
                    var heartbeat = RequestCommand.Heartbeat();
                    SendRequest(heartbeat);

                    if (GameLogger.IsLevelEnabled(GameLogLevel.Verbose))
                    {
                        GameLogger.LogVerbose($"[GameClient] 发送心跳 (待确认: {_heartbeatTimeoutCount})");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[GameClient] 心跳错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 释放心跳相关资源
        /// </summary>
        private void DisposeHeartbeat()
        {
            // StopHeartbeat 已处理 CTS 的释放
            // 此方法保留用于释放额外资源
        }
    }
}
