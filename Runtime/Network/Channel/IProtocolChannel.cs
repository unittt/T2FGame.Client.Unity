using System;

namespace Pisces.Client.Network.Channel
{
    /// <summary>
    /// 发送失败原因
    /// </summary>
    public enum SendFailureReason
    {
        /// <summary>
        /// 未连接
        /// </summary>
        NotConnected,

        /// <summary>
        /// 数据无效
        /// </summary>
        InvalidData,

        /// <summary>
        /// 发送队列已满
        /// </summary>
        QueueFull,

        /// <summary>
        /// 通道已关闭
        /// </summary>
        ChannelClosed,

        /// <summary>
        /// 数据过大
        /// </summary>
        DataTooLarge
    }

    /// <summary>
    ///  通信协议通道的接口
    /// </summary>
    public interface IProtocolChannel : IDisposable
    {
        /// <summary>
        /// 传输协议类型
        /// </summary>
        ChannelType ChannelType { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        void OnInit();

        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        void Connect(string host, int port);

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">要发送的数据（已编码的完整数据包）</param>
        /// <returns>是否成功加入发送队列</returns>
        bool Send(byte[] data);

        /// <summary>
        /// 发送消息成功事件
        /// </summary>
        event Action<IProtocolChannel> SendMessageEvent;

        /// <summary>
        /// 接收消息成功事件
        /// </summary>
        event Action<IProtocolChannel, ArraySegment<byte>> ReceiveMessageEvent;

        /// <summary>
        /// 与服务器断开连接事件
        /// </summary>
        event Action<IProtocolChannel> DisconnectServerEvent;

        /// <summary>
        /// 发送失败事件
        /// </summary>
        event Action<IProtocolChannel, byte[], SendFailureReason> SendFailedEvent;
    }
}
