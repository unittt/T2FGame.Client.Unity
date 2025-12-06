using System;
using System.Collections.Generic;
using Google.Protobuf;
using T2FGame.Client.Protocol;
using T2FGame.Client.Utils;
using T2FGame.Protocol;

namespace T2FGame.Client.Sdk
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

            GameLogger.Log($"[MessageRouter] Subscribed to cmdMerge: {cmdMerge}");
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

            Subscribe(
                cmdMerge,
                message =>
                {
                    try
                    {
                        var typedMessage = ProtoSerializer.Deserialize<TMessage>(message.Data);
                        handler.Invoke(typedMessage);
                    }
                    catch (Exception ex)
                    {
                        GameLogger.LogError(
                            $"[MessageRouter] Failed to unpack message: {ex.Message}"
                        );
                    }
                }
            );
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

            GameLogger.Log($"[MessageRouter] Unsubscribed from cmdMerge: {cmdMerge}");
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
                    $"[MessageRouter] Handler exception for cmdMerge {message.CmdMerge}: {ex.Message}"
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
            GameLogger.Log($"[MessageRouter] Cleared all subscriptions for cmdMerge: {cmdMerge}");
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void ClearAll()
        {
            _routes.Clear();
            GameLogger.Log("[MessageRouter] Cleared all subscriptions");
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
