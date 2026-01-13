using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Sdk;
using Pisces.Client.Utils;
using Pisces.Protocol;
using UnityEngine;

namespace Pisces.Client.Network
{
    /// <summary>
    /// 待处理请求信息
    /// </summary>
    internal sealed class PendingRequestInfo
    {
        public UniTaskCompletionSource<ResponseMessage> Tcs { get; set; }
        public long CreatedTicks { get; set; }
        public int CmdMerge { get; set; }
        public int MsgId { get; set; }

        public void Reset()
        {
            Tcs = null;
            CreatedTicks = 0;
            CmdMerge = 0;
            MsgId = 0;
        }
    }

    /// <summary>
    /// GameClient - 待处理请求管理部分
    /// </summary>
    public partial class GameClient
    {
        /// <summary>
        /// 等待响应的请求队列
        /// Key: MsgId, Value: PendingRequestInfo（包含 TCS 和元数据）
        /// </summary>
        private readonly ConcurrentDictionary<int, PendingRequestInfo> _pendingRequests = new();

        /// <summary>
        /// 清理循环间隔（毫秒）
        /// </summary>
        private const int CleanupIntervalMs = 5000;

        /// <summary>
        /// 请求超时阈值倍数（相对于 RequestTimeoutMs）
        /// 超过此倍数的请求将被强制清理
        /// </summary>
        private const float CleanupTimeoutMultiplier = 2.0f;

        private CancellationTokenSource _cleanupCts;

        /// <summary>
        /// 启动待处理请求清理任务
        /// </summary>
        private void StartPendingRequests()
        {
            StopPendingRequests();

            _cleanupCts = new CancellationTokenSource();
            PendingRequestsCleanupLoop(_cleanupCts.Token).Forget();

            GameLogger.LogDebug("[GameClient] 待处理请求清理任务已启动");
        }

        /// <summary>
        /// 停止待处理请求清理任务
        /// </summary>
        private void StopPendingRequests()
        {
            _cleanupCts?.Cancel();
            _cleanupCts?.Dispose();
            _cleanupCts = null;
        }

        /// <summary>
        /// 待处理请求清理循环
        /// </summary>
        private async UniTaskVoid PendingRequestsCleanupLoop(CancellationToken cancellationToken)
        {
            var timeoutTicks = (long)(_options.RequestTimeoutMs * CleanupTimeoutMultiplier * Stopwatch.Frequency / 1000);

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                try
                {
                    await UniTask.Delay(CleanupIntervalMs, cancellationToken: cancellationToken);

                    if (!Application.isPlaying)
                        break;

                    CleanupStalePendingRequests(timeoutTicks);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[GameClient] 清理待处理请求时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理过期的待处理请求
        /// </summary>
        private void CleanupStalePendingRequests(long timeoutTicks)
        {
            var now = Stopwatch.GetTimestamp();
            var cleanedCount = 0;
            var timedOutCount = 0;

            foreach (var kvp in _pendingRequests)
            {
                var info = kvp.Value;

                // 检查是否已完成但未移除
                if (info.Tcs == null || info.Tcs.Task.Status != UniTaskStatus.Pending)
                {
                    if (_pendingRequests.TryRemove(kvp.Key, out _))
                    {
                        cleanedCount++;
                    }
                    continue;
                }

                // 检查是否超时（基于创建时间）
                var elapsed = now - info.CreatedTicks;
                if (elapsed > timeoutTicks)
                {
                    if (_pendingRequests.TryRemove(kvp.Key, out var removedInfo))
                    {
                        // 以超时异常完成 TCS
                        var timeoutException = new TimeoutException(
                            $"Request cleanup timeout (MsgId: {removedInfo.MsgId}, Cmd: {CmdKit.ToString(removedInfo.CmdMerge)})"
                        );
                        removedInfo.Tcs?.TrySetException(timeoutException);
                        timedOutCount++;

                        GameLogger.LogWarning(
                            $"[GameClient] 强制清理超时请求: MsgId={removedInfo.MsgId}, Cmd={CmdKit.ToString(removedInfo.CmdMerge)}"
                        );
                    }
                }
            }

            if (cleanedCount > 0 || timedOutCount > 0)
            {
                GameLogger.LogVerbose($"[GameClient] 清理待处理请求: 已完成={cleanedCount}, 超时={timedOutCount}");
            }

            // 如果待处理请求数量过多，记录警告
            var pendingCount = _pendingRequests.Count;
            if (pendingCount > 100)
            {
                GameLogger.LogWarning($"[GameClient] 待处理请求数量过多: {pendingCount}");
            }
        }

        /// <summary>
        /// 获取当前待处理请求数量
        /// </summary>
        public int PendingRequestCount => _pendingRequests.Count;

        /// <summary>
        /// 清理所有待处理请求
        /// </summary>
        /// <param name="exception">异常原因</param>
        private void ClearPendingRequests(Exception exception)
        {
            foreach (var kvp in _pendingRequests)
            {
                try
                {
                    kvp.Value.Tcs?.TrySetException(exception);
                }
                catch (Exception ex)
                {
                    GameLogger.LogWarning($"[GameClient] 清理待处理请求时异常: {ex.Message}");
                }
            }
            _pendingRequests.Clear();
        }

        /// <summary>
        /// 释放待处理请求相关资源
        /// </summary>
        private void DisposePendingRequests()
        {
            // StopPendingRequests 已处理 CTS 的释放
            // 此方法保留用于释放额外资源
        }
    }
}
