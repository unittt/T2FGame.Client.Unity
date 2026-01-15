public enum PiscesClientCode
{
    // --- 基础状态 ---
    Success = 0,
    Unknown = 1,
    NotInitialized = 2,
    ClientClosed = 3,

    // --- 连接与生命周期 (100-199) ---
    /// <summary>
    ///  未建立连接
    /// </summary>
    NotConnected = 100,
    /// <summary>
    /// 建立连接物理失败
    /// </summary>
    ConnectionFailed = 101,
    /// <summary>
    /// 握手/逻辑连接失败
    /// </summary>
    HandshakeFailed = 102,
    /// <summary>
    /// 版本不兼容
    /// </summary>
    ProtocolVersionMismatch = 103,
    /// <summary>
    /// 连接被本地异常中止
    /// </summary>
    ConnectionAborted = 104,

    // --- 发送控制 (200-299) ---
    /// <summary>
    /// 请求超时
    /// </summary>
    Timeout = 200,
    /// <summary>
    /// 频率限制
    /// </summary>
    RateLimited = 201,
    /// <summary>
    /// 锁等待
    /// </summary>
    RequestLocked = 202,
    /// <summary>
    /// 重复的消息ID
    /// </summary>
    DuplicateMsgId = 203,
    /// <summary>
    /// CancellationToken 触发
    /// </summary>
    OperationCancelled = 204,
    /// <summary>
    /// 幂等操作正在进行
    /// </summary>
    AlreadyInProgress = 205,

    // --- 数据与协议 (300-399) ---
    /// <summary>
    /// 无效的请求命令
    /// </summary>
    InvalidRequestCommand = 300,
    /// <summary>
    /// 发送前序列化失败
    /// </summary>
    SerializationError = 301,
    /// <summary>
    /// 接收后解析失败
    /// </summary>
    DeserializationError = 302,
    /// <summary>
    /// 数据包超过允许的最大值
    /// </summary>
    PayloadTooLarge = 303,
    /// <summary>
    /// // SDK配置项非法
    /// </summary>
    InvalidConfiguration = 304,

    // --- 底层通道 (400-499) ---
    /// <summary>
    /// 通道错误
    /// </summary>
    ChannelError = 400,
    /// <summary>
    /// 发送缓冲区溢出
    /// </summary>
    BufferFull = 401,
}
