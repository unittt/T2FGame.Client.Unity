using System;
using System.Threading;
using UnityEngine;

namespace Pisces.Client.Utils
{
    /// <summary>
    /// 主线程调度器
    /// 提供将回调调度到 Unity 主线程执行的功能
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static SynchronizationContext _context;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _context != null;

        /// <summary>
        /// 在场景加载前自动初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _context = SynchronizationContext.Current;
        }

        /// <summary>
        /// 在主线程上执行回调
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public static void InvokeOnMainThread(Action action)
        {
            if (action == null)
                return;

            if (_context != null)
            {
                _context.Post(_ => action(), null);
            }
            else
            {
                // 如果没有同步上下文，直接执行（可能在编辑器模式或测试中）
                action();
            }
        }

        /// <summary>
        /// 在主线程上执行回调（带参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="state">传递的参数</param>
        public static void InvokeOnMainThread<T>(Action<T> action, T state)
        {
            if (action == null)
                return;

            if (_context != null)
            {
                _context.Post(_ => action(state), null);
            }
            else
            {
                action(state);
            }
        }
    }
}
