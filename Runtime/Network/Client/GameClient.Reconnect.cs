using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Channel;
using Pisces.Client.Network.Core;
using Pisces.Client.Utils;
using Pisces.Protocol;
using UnityEngine;

namespace Pisces.Client.Network
{
    /// <summary>
    /// GameClient - 重连部分
    /// </summary>
    public partial class GameClient
    {
        // 重连相关
        private CancellationTokenSource _reconnectCts;
        private readonly object _reconnectLock = new();
        private volatile bool _isReconnecting;
        private int _reconnectCount;

        /// <summary>
        /// 判断断线原因是否允许自动重连
        /// </summary>
        /// <param name="reason">断线原因</param>
        /// <returns>是否允许重连</returns>
        private static bool IsReconnectAllowed(DisconnectReason reason)
        {
            return reason switch
            {
                // 不允许重连的情况
                DisconnectReason.DuplicateLogin => false,        // 重复登录（被顶号）
                DisconnectReason.Banned => false,                // 被封禁
                DisconnectReason.ServerMaintenance => false,     // 服务器维护
                DisconnectReason.AuthenticationFailed => false,  // 认证失败
                DisconnectReason.ServerClose => false,           // 服务器关闭

                // 允许重连的情况
                DisconnectReason.Unknown => true,                // 未知原因
                DisconnectReason.ClientClose => true,            // 客户端关闭（一般不会从服务器发来）
                DisconnectReason.IdleTimeout => true,            // 空闲超时
                DisconnectReason.NetworkError => true,           // 网络错误

                // 默认允许重连
                _ => true
            };
        }

        private void StartReconnect()
        {
            if (_isClosed || _disposed)
                return;

            // 使用锁防止并发重连
            lock (_reconnectLock)
            {
                if (_isReconnecting)
                {
                    GameLogger.LogDebug("[GameClient] 已在重连中，跳过");
                    return;
                }
                _isReconnecting = true;
            }

            StopReconnect();

            _reconnectCts = new CancellationTokenSource();
            _stateMachine.TryTransition(ConnectionState.Reconnecting, out _);

            ReconnectLoop(_reconnectCts.Token).Forget();
        }

        private void StopReconnect()
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;
        }

        private async UniTaskVoid ReconnectLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && !_isClosed && Application.isPlaying)
                {
                    // 检查重连次数
                    if (_options.MaxReconnectCount > 0 && _reconnectCount >= _options.MaxReconnectCount)
                    {
                        GameLogger.LogWarning($"[GameClient] 达到最大重连次数 ({_reconnectCount})");
                        _stateMachine.TryTransition(ConnectionState.Disconnected, out _);
                        break;
                    }

                    _reconnectCount++;
                    _statistics.RecordReconnect();
                    GameLogger.Log($"[GameClient] 正在重连... (第 {_reconnectCount} 次尝试)");

                    try
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(_options.ReconnectIntervalSec),
                            cancellationToken: cancellationToken
                        );

                        // 再次检查 Unity 是否仍在运行
                        if (!Application.isPlaying)
                            break;

                        // 转换到 Connecting 状态
                        _stateMachine.TryTransition(ConnectionState.Connecting, out _);

                        // 尝试连接
                        await ConnectInternalAsync();

                        if (IsConnected)
                        {
                            GameLogger.Log("[GameClient] 重连成功");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        GameLogger.LogWarning($"[GameClient] 重连失败: {ex.Message}");
                        _stateMachine.TryTransition(ConnectionState.Reconnecting, out _);
                    }
                }
            }
            finally
            {
                lock (_reconnectLock)
                {
                    _isReconnecting = false;
                }
            }
        }

        /// <summary>
        /// 内部连接方法（重连时调用，跳过状态检查）
        /// </summary>
        private async UniTask ConnectInternalAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameClient));

            // 创建新通道
            var newChannel = ChannelFactory.Create(_options.ChannelType);

            try
            {
                newChannel.OnInit();

                using var cts = new CancellationTokenSource(_options.ConnectTimeoutMs);

                var connectTask = UniTask.RunOnThreadPool(
                    () => newChannel.Connect(_options.Host, _options.Port),
                    cancellationToken: cts.Token
                );

                try
                {
                    await connectTask;
                    await UniTask.WaitUntil(
                        () => newChannel.IsConnected,
                        cancellationToken: cts.Token
                    );
                }
                catch (OperationCanceledException)
                {
                    CleanupChannel(newChannel);
                    throw new TimeoutException(
                        $"Connect timeout after {_options.ConnectTimeoutMs}ms"
                    );
                }

                _reconnectCount = 0;
                // 连接成功，完成后续处理
                FinalizeConnection(newChannel, isReconnect: true);
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CleanupChannel(newChannel);
                GameLogger.LogError($"[GameClient] 连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 释放重连相关资源
        /// </summary>
        private void DisposeReconnect()
        {
            // StopReconnect 已处理 CTS 的释放
            // 此方法保留用于释放额外资源
        }
    }
}
