using System;
using System.Collections.Generic;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// PiscesClientCode 扩展方法
    /// </summary>
    internal static class PiscesClientCodeExtensions
    {
        private static readonly Dictionary<PiscesClientCode, string> _resultMapping = new();

        static PiscesClientCodeExtensions()
        {
            // --- 基础状态 ---
            Mapping(PiscesClientCode.Success, "成功");
            Mapping(PiscesClientCode.Unknown, "未知错误");
            Mapping(PiscesClientCode.NotInitialized, "SDK 未初始化");
            Mapping(PiscesClientCode.ClientClosed, "客户端已关闭");

            // --- 连接与生命周期 ---
            Mapping(PiscesClientCode.NotConnected, "未连接到服务器");
            Mapping(PiscesClientCode.ConnectionFailed, "网络连接建立失败");
            Mapping(PiscesClientCode.HandshakeFailed, "协议握手失败");
            Mapping(PiscesClientCode.ProtocolVersionMismatch, "协议版本不匹配");

            // --- 发送控制与策略 ---
            Mapping(PiscesClientCode.Timeout, "请求超时");
            Mapping(PiscesClientCode.RateLimited, "发送频率超限");
            Mapping(PiscesClientCode.RequestLocked, "请求已锁定（重复请求）");
            Mapping(PiscesClientCode.DuplicateMsgId, "重复的消息ID");
            Mapping(PiscesClientCode.OperationCancelled, "操作已取消");

            // --- 数据与协议逻辑 ---
            Mapping(PiscesClientCode.InvalidRequestCommand, "无效的请求指令");
            Mapping(PiscesClientCode.SerializationError, "数据序列化失败");
            Mapping(PiscesClientCode.DeserializationError, "数据反序列化失败");
            Mapping(PiscesClientCode.PayloadTooLarge, "数据包大小超限");

            // --- 底层通道 ---
            Mapping(PiscesClientCode.ChannelError, "通道发送异常");
            Mapping(PiscesClientCode.BufferFull, "本地发送缓冲区已满");
        }

        private static void Mapping(PiscesClientCode result, string msg)
        {
            _resultMapping[result] = msg;
        }

        public static string GetMessage(this PiscesClientCode result)
        {
            if (!_resultMapping.TryGetValue(result, out var msg))
            {
                msg = result.ToString();
            }
            return msg;
        }


        #region  异常处理

        /// <summary>
        /// 如果状态码不是成功，则抛出异常
        /// </summary>
        public static void ThrowIfFailed(this PiscesClientCode clientCode, string message = null,Exception inner = null)
        {
            if (clientCode != PiscesClientCode.Success)
            {
                throw new PiscesClientException(clientCode, message,inner);
            }
        }

        /// <summary>
        /// 如果状态码不是成功，则抛出包含命令上下文的异常
        /// </summary>
        public static void ThrowIfFailed(this PiscesClientCode clientCode, RequestCommand command, string message = null,Exception inner = null)
        {
            if (clientCode != PiscesClientCode.Success)
            {
                throw new PiscesClientException(clientCode, command, message,inner);
            }
        }
        #endregion
    }
}
