using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network.Core;
using Pisces.Client.Sdk;
using Pisces.Protocol;

namespace Pisces.Client.Network
{
    /// <summary>
    /// 游戏客户端接口
    /// </summary>
    public interface IGameClient : IDisposable
    {
        /// <summary>
        /// 当前连接状态
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 客户端配置
        /// </summary>
        GameClientOptions Options { get; }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        UniTask ConnectAsync();

        /// <summary>
        /// 断开连接
        /// </summary>
        UniTask DisconnectAsync();

        /// <summary>
        /// 关闭连接（不再重连）
        /// </summary>
        void Close();

        /// <summary>
        ///  发送请求
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        PiscesCode SendRequest(RequestCommand command);

        /// <summary>
        ///  发送请求
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<ResponseMessage> RequestAsync(RequestCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event Action<ConnectionState> OnStateChanged;

        /// <summary>
        /// 收到消息事件
        /// </summary>
        event Action<ResponseMessage> OnMessageReceived;

        /// <summary>
        /// 连接错误事件
        /// </summary>
        event Action<Exception> OnError;
    }
}
