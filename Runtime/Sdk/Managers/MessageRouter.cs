using System;
using System.Collections.Generic;
using Google.Protobuf;
using Pisces.Protocol;
using Pisces.Client.Utils;

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
        private readonly Dictionary<int, Action<ExternalMessage>> _routes = new();

        /// <summary>
        /// 泛型 handler 到包装后 handler 的映射表
        /// Key: 原始泛型 handler 的 hashcode + cmdMerge 组合
        /// Value: 包装后的 Action&lt;ExternalMessage&gt;
        /// </summary>
        private readonly Dictionary<(int cmdMerge, Delegate handler), Action<ExternalMessage>> _handlerWrappers = new();

        /// <summary>
        /// 订阅指定 cmdMerge 的消息
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        public void Subscribe(int cmdMerge, Action<ExternalMessage> handler)
        {
            if (handler == null)
                return;

            if (_routes.TryGetValue(cmdMerge, out var existing))
            {
                _routes[cmdMerge] = existing + handler;
            }
            else
            {
                _routes[cmdMerge] = handler;
            }

            GameLogger.Log($"[MessageRouter] 已订阅 cmdMerge: {cmdMerge}");
        }

        /// <summary>
        /// 订阅指定 cmdMerge 的消息（泛型版本，自动解包）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        public void Subscribe<TMessage>(int cmdMerge, Action<TMessage> handler)
            where TMessage : IMessage, new()
        {
            if (handler == null)
                return;

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

            // 保存映射关系，用于后续取消订阅
            _handlerWrappers[(cmdMerge, handler)] = wrapper;

            Subscribe(cmdMerge, wrapper);
        }

        /// <summary>
        /// 取消订阅指定 cmdMerge 的消息
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        public void Unsubscribe(int cmdMerge, Action<ExternalMessage> handler)
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

            GameLogger.Log($"[MessageRouter] 已取消订阅 cmdMerge: {cmdMerge}");
        }

        /// <summary>
        /// 取消订阅指定 cmdMerge 的消息（泛型版本）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="cmdMerge">命令路由标识</param>
        /// <param name="handler">消息处理回调</param>
        public void Unsubscribe<TMessage>(int cmdMerge, Action<TMessage> handler)
            where TMessage : IMessage, new()
        {
            if (handler == null)
                return;

            var key = (cmdMerge, (Delegate)handler);
            if (!_handlerWrappers.TryGetValue(key, out var wrapper))
            {
                GameLogger.LogWarning($"[MessageRouter] 未找到对应的订阅 cmdMerge: {cmdMerge}");
                return;
            }

            // 取消订阅包装器
            Unsubscribe(cmdMerge, wrapper);

            // 移除映射关系
            _handlerWrappers.Remove(key);
        }

        /// <summary>
        /// 分发消息到订阅者
        /// </summary>
        /// <param name="message">接收到的消息</param>
        public void Dispatch(ExternalMessage message)
        {
            if (message == null)
                return;

            if (!_routes.TryGetValue(message.CmdMerge, out var handler) || handler == null)
                return;

            try
            {
                handler.Invoke(message);
            }
            catch (Exception ex)
            {
                GameLogger.LogError(
                    $"[MessageRouter] 处理器异常 cmdMerge {message.CmdMerge}: {ex.Message}"
                );
                throw; // 重新抛出异常，让外层处理
            }
        }

        /// <summary>
        /// 清除指定 cmdMerge 的所有订阅
        /// </summary>
        /// <param name="cmdMerge">命令路由标识</param>
        public void Clear(int cmdMerge)
        {
            _routes.Remove(cmdMerge);

            // 清除该 cmdMerge 相关的包装器映射
            var keysToRemove = new List<(int, Delegate)>();
            foreach (var key in _handlerWrappers.Keys)
            {
                if (key.cmdMerge == cmdMerge)
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _handlerWrappers.Remove(key);
            }

            GameLogger.Log($"[MessageRouter] 已清除 cmdMerge 的所有订阅: {cmdMerge}");
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void ClearAll()
        {
            _routes.Clear();
            _handlerWrappers.Clear();
            GameLogger.Log("[MessageRouter] 已清除所有订阅");
        }

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
    }
}
