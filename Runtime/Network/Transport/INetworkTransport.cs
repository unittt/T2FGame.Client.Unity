using System.Threading;
using Cysharp.Threading.Tasks;

namespace T2FGame.Client.Network
{
    public interface INetworkTransport
    {
        /// <summary>
        /// 传输协议类型
        /// </summary>
        TransportType TransportType { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开连接
        /// </summary>
        UniTask DisconnectAsync();
    }
}