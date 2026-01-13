using System;
using System.Collections.Generic;
using Pisces.Client.Utils;

namespace Pisces.Client.Network.Core
{
    /// <summary>
    /// 连接状态机
    /// 管理连接状态的转换，确保状态转换的合法性和原子性
    /// </summary>
    public sealed class ConnectionStateMachine
    {
        /// <summary>
        /// 合法的状态转换规则表
        /// Key: 当前状态, Value: 允许转换到的目标状态集合
        /// </summary>
        private static readonly Dictionary<ConnectionState, HashSet<ConnectionState>> _validTransitions = new()
        {
            [ConnectionState.Disconnected] = new HashSet<ConnectionState>
            {
                ConnectionState.Connecting,
                ConnectionState.Closed
            },
            [ConnectionState.Connecting] = new HashSet<ConnectionState>
            {
                ConnectionState.Connected,
                ConnectionState.Disconnected,
                ConnectionState.Closed
            },
            [ConnectionState.Connected] = new HashSet<ConnectionState>
            {
                ConnectionState.Disconnected,
                ConnectionState.Reconnecting,
                ConnectionState.Closed
            },
            [ConnectionState.Reconnecting] = new HashSet<ConnectionState>
            {
                ConnectionState.Connecting,
                ConnectionState.Disconnected,
                ConnectionState.Closed
            },
            [ConnectionState.Closed] = new HashSet<ConnectionState>()
            // Closed 是终态，不允许转换到任何其他状态
        };

        private readonly object _stateLock = new();
        private ConnectionState _state = ConnectionState.Disconnected;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event Action<ConnectionState, ConnectionState> OnStateChanged;

        /// <summary>
        /// 当前状态
        /// </summary>
        public ConnectionState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// 是否处于已连接状态
        /// </summary>
        public bool IsConnected => CurrentState == ConnectionState.Connected;

        /// <summary>
        /// 是否处于终态（Closed）
        /// </summary>
        public bool IsClosed => CurrentState == ConnectionState.Closed;

        /// <summary>
        /// 是否可以开始连接
        /// </summary>
        public bool CanConnect
        {
            get
            {
                var state = CurrentState;
                return state == ConnectionState.Disconnected;
            }
        }

        /// <summary>
        /// 是否正在连接或重连中
        /// </summary>
        public bool IsConnectingOrReconnecting
        {
            get
            {
                var state = CurrentState;
                return state == ConnectionState.Connecting || state == ConnectionState.Reconnecting;
            }
        }

        /// <summary>
        /// 尝试转换到新状态
        /// </summary>
        /// <param name="newState">目标状态</param>
        /// <param name="oldState">输出：转换前的状态</param>
        /// <returns>是否转换成功</returns>
        public bool TryTransition(ConnectionState newState, out ConnectionState oldState)
        {
            lock (_stateLock)
            {
                oldState = _state;

                // 相同状态不需要转换
                if (_state == newState)
                    return true;

                // 检查是否是合法的状态转换
                if (!IsValidTransition(_state, newState))
                {
                    GameLogger.LogWarning(
                        $"[StateMachine] 非法状态转换: {_state} -> {newState}"
                    );
                    return false;
                }

                // 执行状态转换
                var previousState = _state;
                _state = newState;

                // GameLogger.Log($"[StateMachine] 状态转换: {previousState} -> {newState}");

                // 触发事件（在锁外触发以避免死锁）
                // 注意：这里我们在锁内触发，因为事件处理程序应该是轻量级的
                // 如果需要在锁外触发，可以使用委托缓存
                OnStateChanged?.Invoke(previousState, newState);

                return true;
            }
        }

        /// <summary>
        /// 强制转换到新状态（跳过验证）
        /// 仅在特殊情况下使用，如初始化或错误恢复
        /// </summary>
        /// <param name="newState">目标状态</param>
        /// <param name="reason">强制转换的原因（用于日志）</param>
        public void ForceTransition(ConnectionState newState, string reason = null)
        {
            lock (_stateLock)
            {
                var previousState = _state;
                _state = newState;

                var reasonText = string.IsNullOrEmpty(reason) ? "" : $" (原因: {reason})";
                GameLogger.LogWarning(
                    $"[StateMachine] 强制状态转换: {previousState} -> {newState}{reasonText}"
                );

                OnStateChanged?.Invoke(previousState, newState);
            }
        }

        /// <summary>
        /// 检查从当前状态是否可以转换到目标状态
        /// </summary>
        /// <param name="from">源状态</param>
        /// <param name="to">目标状态</param>
        /// <returns>是否是合法转换</returns>
        public static bool IsValidTransition(ConnectionState from, ConnectionState to)
        {
            return _validTransitions.TryGetValue(from, out var validTargets)
                   && validTargets.Contains(to);
        }

        /// <summary>
        /// 获取从当前状态可以转换到的所有合法状态
        /// </summary>
        /// <returns>合法的目标状态集合</returns>
        public IReadOnlyCollection<ConnectionState> GetValidTransitions()
        {
            lock (_stateLock)
            {
                return _validTransitions.TryGetValue(_state, out var validTargets)
                    ? validTargets
                    : Array.Empty<ConnectionState>();
            }
        }

        /// <summary>
        /// 重置状态机到初始状态
        /// </summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                var previousState = _state;
                _state = ConnectionState.Disconnected;

                if (previousState != ConnectionState.Disconnected)
                {
                    GameLogger.LogDebug($"[StateMachine] 重置: {previousState} -> Disconnected");
                    OnStateChanged?.Invoke(previousState, ConnectionState.Disconnected);
                }
            }
        }

        /// <summary>
        /// 在指定状态下执行操作（状态锁定）
        /// </summary>
        /// <param name="expectedState">期望的状态</param>
        /// <param name="action">要执行的操作</param>
        /// <returns>是否成功执行（状态匹配时执行）</returns>
        public bool ExecuteInState(ConnectionState expectedState, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (_stateLock)
            {
                if (_state != expectedState)
                    return false;

                action();
                return true;
            }
        }

        /// <summary>
        /// 原子性地检查当前状态并转换
        /// </summary>
        /// <param name="expectedState">期望的当前状态</param>
        /// <param name="newState">目标状态</param>
        /// <returns>是否成功转换</returns>
        public bool CompareAndTransition(ConnectionState expectedState, ConnectionState newState)
        {
            lock (_stateLock)
            {
                if (_state != expectedState)
                    return false;

                return TryTransition(newState, out _);
            }
        }
    }
}
