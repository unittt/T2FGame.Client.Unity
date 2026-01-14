using System;
using System.Collections.Generic;
using Google.Protobuf;
using Pisces.Client.Utils;
using Pisces.Protocol;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 消息路由管理器
    /// 负责管理 cmdMerge 消息订阅和分发
    /// 设计为单线程使用（Unity 主线程），不需要线程安全
    /// </summary>
    internal sealed class MessageRouter
    {
        /// <summary>
        /// cmdMerge -> 回调委托的映射表
        /// 使用 multicast delegate 实现高效的订阅机制
        /// </summary>
        private readonly Dictionary<CmdInfo, Action<ExternalMessage>> _routes = new();

        /// <summary>
        /// 分发异常事件
        /// 当订阅者处理消息时发生异常时触发
        /// 参数：cmdMerge, 异常
        /// </summary>
        public event Action<CmdInfo, Exception> OnDispatchError;

        #region Subscribe - 返回 IDisposable

        /// <summary>
        /// 订阅指定 cmdMerge 的消息
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe(int cmdMerge, Action<ExternalMessage> handler)
        {
            if (handler == null)
                return Subscription.Empty;

            if (_routes.TryGetValue(cmdMerge, out var existing))
            {
                _routes[cmdMerge] = existing + handler;
            }
            else
            {
                _routes[cmdMerge] = handler;
            }

            GameLogger.LogVerbose($"[MessageRouter] 订阅 cmdMerge: {CmdKit.ToString(cmdMerge)}");

            return new Subscription(this, cmdMerge, handler);
        }

        /// <summary>
        /// 订阅指定 cmdMerge 的消息（泛型版本，自动解包）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe<TMessage>(int cmdMerge, Action<TMessage> handler)
            where TMessage : IMessage, new()
        {
            if (handler == null)
                return Subscription.Empty;

            // 创建包装器
            Action<ExternalMessage> wrapper = message =>
            {
                try
                {
                    var typedMessage = ProtoSerializer.Deserialize<TMessage>(message.Data);
                    handler.Invoke(typedMessage);
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[MessageRouter] 解包消息失败: {ex.Message}");
                }
            };

            return Subscribe(cmdMerge, wrapper);
        }

        /// <summary>
        /// 订阅指定 cmdMerge 的消息（使用 MessageParser，性能更优）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        /// <param name="parser">消息解析器，通常使用 YourMessage.Parser</param>
        /// <returns>用于取消订阅的 IDisposable</returns>
        public IDisposable Subscribe<TMessage>(int cmdMerge, Action<TMessage> handler, MessageParser<TMessage> parser)
            where TMessage : IMessage<TMessage>
        {
            if (handler == null || parser == null)
                return Subscription.Empty;

            // 创建包装器，使用 MessageParser 解析
            Action<ExternalMessage> wrapper = message =>
            {
                try
                {
                    var typedMessage = ProtoSerializer.Deserialize(message.Data, parser);
                    handler.Invoke(typedMessage);
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[MessageRouter] 解包消息失败: {ex.Message}");
                }
            };

            return Subscribe(cmdMerge, wrapper);
        }

        #endregion

        #region Unsubscribe - 内部使用

        /// <summary>
        /// 取消订阅指定 cmdMerge 的消息（内部使用，由 Subscription.Dispose 调用）
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        internal void Unsubscribe(int cmdMerge, Action<ExternalMessage> handler)
        {
            if (handler == null)
                return;

            if (!_routes.TryGetValue(cmdMerge, out var existing))
                return;

            var updated = existing - handler;
            if (updated == null)
            {
                _routes.Remove(cmdMerge);
            }
            else
            {
                _routes[cmdMerge] = updated;
            }

            GameLogger.LogVerbose($"[MessageRouter] 取消订阅 cmdMerge: {CmdKit.ToString(cmdMerge)}");
        }

        #endregion

        #region Dispatch

        /// <summary>
        /// 分发消息到订阅者
        /// 使用 GetInvocationList 遍历所有订阅者，确保单个订阅者异常不影响其他订阅者
        /// </summary>
        /// <param name="message">接收到的消息</param>
        public void Dispatch(ExternalMessage message)
        {
            if (message == null)
                return;

            if (!_routes.TryGetValue(message.CmdMerge, out var handler) || handler == null)
                return;

            // 获取所有订阅者
            var invocationList = handler.GetInvocationList();

            foreach (var subscriber in invocationList)
            {
                try
                {
                    ((Action<ExternalMessage>)subscriber).Invoke(message);
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[MessageRouter] 处理器异常 cmdMerge {message.CmdInfo.ToString()}: {ex.Message}\n{ex.StackTrace}");

                    // 触发异常事件，让外部可以处理（如统计、上报等）
                    try
                    {
                        OnDispatchError?.Invoke(message.CmdInfo, ex);
                    }
                    catch
                    {
                        // 忽略事件处理器自身的异常
                    }

                    // 继续处理其他订阅者，不中断分发流程
                }
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// 清除指定 cmdMerge 的所有订阅
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        public void Clear(int cmdMerge)
        {
            _routes.Remove(cmdMerge);
            GameLogger.LogDebug($"[MessageRouter] 清除 cmdMerge 订阅: {CmdKit.ToString(cmdMerge)}");
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void ClearAll()
        {
            _routes.Clear();
            GameLogger.LogDebug("[MessageRouter] 清除所有订阅");
        }

        #endregion

        #region Query

        /// <summary>
        /// 获取指定 cmdMerge 的订阅数量
        /// </summary>
        public int GetSubscriberCount(int cmdMerge)
        {
            if (!_routes.TryGetValue(cmdMerge, out var handler) || handler == null)
                return 0;

            return handler.GetInvocationList().Length;
        }

        /// <summary>
        /// 检查是否有订阅者
        /// </summary>
        public bool HasSubscribers(int cmdMerge)
        {
            return _routes.TryGetValue(cmdMerge, out var handler) && handler != null;
        }

        #endregion

        #region Subscription

        /// <summary>
        /// 订阅凭证，用于取消订阅
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            /// <summary>
            /// 空订阅（用于无效参数情况）
            /// </summary>
            public static readonly IDisposable Empty = new EmptySubscription();

            private MessageRouter _router;
            private readonly int _cmdMerge;
            private Action<ExternalMessage> _handler;
            private bool _disposed;

            public Subscription(MessageRouter router, int cmdMerge, Action<ExternalMessage> handler)
            {
                _router = router;
                _cmdMerge = cmdMerge;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _router?.Unsubscribe(_cmdMerge, _handler);

                // 清理引用，帮助 GC
                _router = null;
                _handler = null;
            }
        }

        /// <summary>
        /// 空订阅实现
        /// </summary>
        private sealed class EmptySubscription : IDisposable
        {
            public void Dispose() { }
        }

        #endregion
    }
}
