namespace T2FGame.Client.Network
{
    /// <summary>
    /// 游戏客户端接口
    /// </summary>
    public class IGameClient
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
    }
}