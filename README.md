# Pisces Client SDK

<div align="center">

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**高性能、模块化、跨平台的 Unity 游戏客户端网络 SDK**

</div>

---

## 目录

- [项目简介](#-项目简介)
- [架构设计](#-架构设计)
- [安装](#-安装)
- [快速开始](#-快速开始)
- [核心功能](#-核心功能)
- [可靠性与性能](#-可靠性与性能)
- [API 参考](#-api-参考)
- [协议规范](#-协议规范)
- [服务器对接](#-服务器对接)
- [编辑器工具](#-编辑器工具)
- [平台适配](#-平台适配)
- [常见问题](#-常见问题)

---

## 项目简介

Pisces Client SDK 是专为 Unity 设计的**高性能网络通信框架**。基于 **UniTask + Protobuf**，支持 TCP/UDP/WebSocket 多协议，提供心跳保活、自动重连、时间同步等开箱即用的功能。

| 特性 | 说明 |
|------|------|
| **零业务耦合** | 纯网络层，可集成到任意 Unity 项目 |
| **高性能** | 基于 UniTask 异步编程，对象池减少 GC |
| **跨平台** | TCP、UDP、WebSocket，适配所有平台 |
| **可靠性** | 自动重连、心跳保活、断线通知、异常隔离 |
| **时间同步** | 客户端与服务器时钟同步，RTT 测量 |
| **流量控制** | 令牌桶限流、发送失败通知 |
| **智能内存** | PacketBuffer 自动扩缩容，降低内存占用 |
| **Unity 集成** | 自动生命周期管理，编辑器友好 |

---

## 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  业务层 (Game Logic)                                     │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  PiscesSdk (Facade)                                     │
│  ├─ ConnectionManager   (连接生命周期)                   │
│  ├─ MessageRouter       (消息订阅/分发，异常隔离)         │
│  └─ RequestManager      (请求/响应处理)                  │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  GameClient (网络核心，模块化设计)                        │
│  ├─ ConnectionStateMachine (状态机，状态转换验证)         │
│  ├─ GameClient.Messaging   (消息收发，发送失败通知)       │
│  ├─ GameClient.Heartbeat   (心跳保活，超时检测)           │
│  ├─ GameClient.Reconnect   (自动重连，指数退避)           │
│  ├─ GameClient.PendingRequests (待处理请求自动清理)       │
│  └─ RateLimiter            (令牌桶限流)                  │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  Protocol Channel (传输层，实现 IDisposable)             │
│  ├─ TcpChannel      (可靠、有序，支持 DNS/IPv6)          │
│  ├─ UdpChannel      (低延迟，支持 DNS/IPv6)              │
│  └─ WebSocketChannel (WebGL 兼容，需启用宏)              │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  PacketCodec (协议编解码)                                │
│  ├─ PacketBuffer (智能缓冲区，自动扩缩容)                 │
│  └─ [4字节长度头] + [ExternalMessage (Protobuf)]         │
└─────────────────────────────────────────────────────────┘
```

---

## 安装

### Git URL（推荐）

```
https://github.com/PiscesGameDev/Pisces.Client.Unity.git
```

### 依赖项

| 依赖 | 版本 | 说明 |
|------|------|------|
| [**UniTask**](https://github.com/Cysharp/UniTask) | 2.3.3+ | 异步编程框架（必需） |
| [**Protobuf**](https://github.com/protocolbuffers/protobuf) | 3.x | 消息序列化（必需） |
| [**UnityWebSocket**](https://github.com/psygames/UnityWebSocket) | 2.8.6+ | WebGL 平台可选 |

---

## 快速开始

### 1. 初始化与连接

```csharp
// 配置
var options = new GameClientOptions
{
    Host = "127.0.0.1",
    Port = 10100,
    AutoReconnect = true,
    HeartbeatIntervalSec = 30
};

// 初始化
PiscesSdk.Instance.Initialize(options);

// 订阅事件
PiscesSdk.Instance.OnStateChanged += state => Debug.Log($"连接状态: {state}");
PiscesSdk.Instance.OnDisconnectNotify += notify =>
{
    Debug.Log($"断线原因: {notify.Reason}, 消息: {notify.Message}");
};
PiscesSdk.Instance.OnError += ex => Debug.LogError($"错误: {ex.Message}");

// 连接
await PiscesSdk.Instance.ConnectAsync();

// 时间同步（可选）
PiscesSdk.Instance.RequestTimeSync();
```

### 2. 发送请求

```csharp
// 方式 1: 异步请求（推荐）
var response = await PiscesSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
    CmdKit.Merge(1, 1),  // cmd=1, subCmd=1
    new LoginRequest { Username = "player1" }
);
Debug.Log($"登录成功: {response.UserId}");

// 方式 2: 回调模式
PiscesSdk.Instance.Send<LoginRequest, LoginResponse>(
    CmdKit.Merge(1, 1),
    request,
    response => Debug.Log($"UserId: {response.UserId}")
);

// 方式 3: 仅发送（不等待响应）
PiscesSdk.Instance.Send(CmdKit.Merge(2, 1), chatMessage);

// 方式 4: 发送基础类型
PiscesSdk.Instance.Send(CmdKit.Merge(3, 1), 12345);      // int
PiscesSdk.Instance.Send(CmdKit.Merge(3, 2), "hello");    // string
PiscesSdk.Instance.Send(CmdKit.Merge(3, 3), position);   // Vector3
```

### 3. 订阅推送消息

```csharp
// 订阅（使用 MessageParser，性能最优）
IDisposable subscription = PiscesSdk.Instance.Subscribe(
    CmdKit.Merge(10, 1),
    (ChatMessage msg) => Debug.Log($"收到消息: {msg.Content}"),
    ChatMessage.Parser
);

// 订阅（自动反序列化）
IDisposable subscription2 = PiscesSdk.Instance.Subscribe<ChatMessage>(
    CmdKit.Merge(10, 1),
    msg => Debug.Log($"收到消息: {msg.Content}")
);

// 订阅原始消息
IDisposable subscription3 = PiscesSdk.Instance.Subscribe(
    CmdKit.Merge(10, 2),
    (ExternalMessage msg) => { var data = msg.Data; }
);

// 取消订阅（调用 Dispose）
subscription.Dispose();

// 取消所有订阅
PiscesSdk.Instance.UnsubscribeAll(CmdKit.Merge(10, 1));
```

### 4. 断开连接

```csharp
// 优雅断开（可重连）
await PiscesSdk.Instance.DisconnectAsync();

// 关闭（不再重连）
PiscesSdk.Instance.Close();

// 释放资源
PiscesSdk.Instance.Dispose();
```

---

## 核心功能

### 连接状态

```csharp
public enum ConnectionState
{
    Disconnected,   // 未连接
    Connecting,     // 连接中
    Connected,      // 已连接
    Reconnecting,   // 重连中
    Closed          // 已关闭（不再重连）
}

// 监听状态变化
PiscesSdk.Instance.OnStateChanged += state =>
{
    switch (state)
    {
        case ConnectionState.Connected:
            // 重连成功，同步数据
            SyncGameData();
            break;
        case ConnectionState.Disconnected:
            // 显示断线 UI
            ShowDisconnectUI();
            break;
    }
};
```

### 智能连接管理

`ConnectionManager` 提供智能连接复用：

```csharp
// 首次连接 - 创建新 GameClient
await PiscesSdk.Instance.ConnectAsync("192.168.1.100", 10100);

// 重复连接相同地址 - 跳过，直接返回
await PiscesSdk.Instance.ConnectAsync("192.168.1.100", 10100);
// 输出: [ConnectionManager] 已连接到 192.168.1.100:10100，跳过重复连接

// 连接不同地址 - 断开旧连接，重建客户端
await PiscesSdk.Instance.ConnectAsync("192.168.1.200", 10100);
```

### 断线通知

服务器主动断开连接时，会发送断线原因：

```csharp
PiscesSdk.Instance.OnDisconnectNotify += notify =>
{
    switch (notify.Reason)
    {
        case DisconnectReason.DuplicateLogin:
            ShowDialog("您的账号在其他设备登录");
            break;
        case DisconnectReason.Banned:
            ShowDialog($"账号已被封禁: {notify.Message}");
            break;
        case DisconnectReason.ServerMaintenance:
            var time = DateTimeOffset.FromUnixTimeMilliseconds(notify.EstimatedRecoveryTime);
            ShowDialog($"服务器维护中，预计 {time:HH:mm} 恢复");
            break;
        case DisconnectReason.IdleTimeout:
            // 允许自动重连
            break;
    }
};
```

**断线原因与重连策略：**

| 原因 | 自动重连 | 说明 |
|------|----------|------|
| `DuplicateLogin` | ❌ | 被顶号 |
| `Banned` | ❌ | 被封禁 |
| `ServerMaintenance` | ❌ | 服务器维护 |
| `AuthenticationFailed` | ❌ | 认证失败 |
| `ServerClose` | ❌ | 服务器关闭 |
| `IdleTimeout` | ✅ | 空闲超时 |
| `NetworkError` | ✅ | 网络错误 |
| `Unknown` | ✅ | 未知原因 |

### 时间同步

```csharp
// 请求时间同步
PiscesSdk.Instance.RequestTimeSync();

// 检查是否已同步
if (PiscesSdk.Instance.IsTimeSynced)
{
    // 获取服务器时间
    DateTime serverTime = PiscesSdk.Instance.ServerTime;
    long serverTimeMs = PiscesSdk.Instance.ServerTimeMs;

    // 获取网络延迟
    float rtt = PiscesSdk.Instance.RttMs;

    Debug.Log($"服务器时间: {serverTime}, RTT: {rtt}ms");
}

// 也可直接使用 TimeUtils
long serverMs = TimeUtils.ServerTimeMs;
DateTime serverDt = TimeUtils.ServerTime;
bool synced = TimeUtils.IsSynced;
```

### 心跳保活

心跳自动管理，无需手动处理：

```csharp
var options = new GameClientOptions
{
    HeartbeatIntervalSec = 30,   // 每 30 秒发送心跳
    HeartbeatTimeoutCount = 3    // 连续 3 次超时则断开
};
```

### 自动重连

```csharp
var options = new GameClientOptions
{
    AutoReconnect = true,        // 启用自动重连
    ReconnectIntervalSec = 3,    // 重连间隔 3 秒
    MaxReconnectCount = 5        // 最多重试 5 次（0 = 无限）
};
```

---

## 可靠性与性能

### 连接状态机

使用状态机管理连接状态，确保状态转换的合法性：

```csharp
// 合法的状态转换
Disconnected → Connecting → Connected → Disconnected
Connected → Reconnecting → Connected

// 非法转换会被阻止，避免状态混乱
Disconnected → Connected  // ❌ 必须先经过 Connecting
```

### 消息路由异常隔离

单个订阅者的异常不会影响其他订阅者接收消息：

```csharp
// 订阅同一个 cmdMerge 的多个处理器
sdk.Subscribe(cmdMerge, msg => HandleA(msg));  // 处理器 A
sdk.Subscribe(cmdMerge, msg => throw new Exception());  // 处理器 B 抛异常
sdk.Subscribe(cmdMerge, msg => HandleC(msg));  // 处理器 C 仍能收到消息

// 监听分发异常
sdk.Client.MessageRouter.OnDispatchError += (cmdMerge, ex) =>
{
    Debug.LogError($"消息处理异常 {CmdKit.ToString(cmdMerge)}: {ex.Message}");
};
```

### 发送失败通知

当消息发送失败时，可以收到通知：

```csharp
// 发送结果
public enum SendResult
{
    Success,         // 成功
    NotConnected,    // 未连接
    RateLimited,     // 被限流
    ClientClosed,    // 客户端已关闭
    InvalidMessage,  // 无效消息
    ChannelError     // 通道错误
}

// 监听发送失败
sdk.OnSendFailed += (cmdMerge, msgId, result) =>
{
    Debug.LogWarning($"发送失败 {CmdKit.ToString(cmdMerge)}: {result}");
};
```

### 通道层发送失败

通道层（Channel）也支持发送失败通知：

```csharp
// 发送失败原因
public enum SendFailureReason
{
    NotConnected,    // 未连接
    InvalidData,     // 数据无效
    QueueFull,       // 发送队列已满
    ChannelClosed    // 通道已关闭
}

// 通道会在发送失败时触发 SendFailedEvent
channel.SendFailedEvent += (channel, data, reason) =>
{
    Debug.LogWarning($"通道发送失败: {reason}");
};
```

### 令牌桶限流

使用令牌桶算法防止发送消息过于频繁：

```csharp
var options = new GameClientOptions
{
    EnableRateLimit = true,   // 启用限流
    MaxSendRate = 100,        // 每秒最多 100 条消息
    MaxBurstSize = 50         // 允许突发 50 条
};

// 超过限制的消息会返回 SendResult.RateLimited
```

### 智能缓冲区管理

PacketBuffer 自动管理内存，避免浪费：

```csharp
var options = new GameClientOptions
{
    PacketBufferInitialSize = 4096,      // 初始 4KB（覆盖大多数消息）
    PacketBufferShrinkThreshold = 65536  // 超过 64KB 且使用率低时自动收缩
};

// 相比固定 64KB 缓冲区，可节省 93% 初始内存
```

### 资源自动释放

所有通道实现 `IDisposable`，确保资源正确释放：

```csharp
// 通道层
public interface IProtocolChannel : IDisposable
{
    // ...
}

// PiscesSdk.Dispose() 会自动释放所有资源
// 包括 ConnectionManager → GameClient → IProtocolChannel
```

---

## API 参考

### PiscesSdk

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `PiscesSdk` | 单例实例 |
| `Client` | `GameClient` | 底层客户端 |
| `State` | `ConnectionState` | 连接状态 |
| `IsConnected` | `bool` | 是否已连接 |
| `IsInitialized` | `bool` | 是否已初始化 |
| `IsTimeSynced` | `bool` | 是否已时间同步 |
| `RttMs` | `float` | 网络延迟（毫秒） |
| `ServerTimeMs` | `long` | 服务器时间戳 |
| `ServerTime` | `DateTime` | 服务器时间 |

#### 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| `OnStateChanged` | `ConnectionState` | 连接状态变化 |
| `OnMessageReceived` | `ExternalMessage` | 收到原始消息 |
| `OnDisconnectNotify` | `DisconnectNotify` | 服务器断线通知 |
| `OnError` | `Exception` | 发生错误 |

#### 方法

| 方法 | 说明 |
|------|------|
| `Initialize(options)` | 初始化 SDK |
| `ConnectAsync()` | 连接服务器 |
| `ConnectAsync(host, port)` | 连接指定服务器 |
| `DisconnectAsync()` | 断开连接 |
| `Close()` | 关闭连接（不再重连） |
| `RequestTimeSync()` | 请求时间同步 |
| `RequestAsync<TReq, TResp>(cmd, req)` | 异步请求 |
| `Send(cmd, msg)` | 仅发送消息 |
| `Send<TReq, TResp>(cmd, req, callback)` | 回调模式 |
| `Subscribe(cmd, handler)` | 订阅推送，返回 `IDisposable` |
| `Subscribe<T>(cmd, handler)` | 泛型订阅，返回 `IDisposable` |
| `Subscribe<T>(cmd, handler, parser)` | 使用 MessageParser 订阅（性能更优） |
| `UnsubscribeAll(cmd)` | 取消指定命令的所有订阅 |
| `UnsubscribeAll()` | 取消所有订阅 |
| `Dispose()` | 释放资源 |

### GameClientOptions

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `ChannelType` | `ChannelType` | `Tcp` | 传输协议 |
| `Host` | `string` | `localhost` | 服务器地址（支持域名） |
| `Port` | `int` | `9090` | 服务器端口 |
| `ConnectTimeoutMs` | `int` | `10000` | 连接超时（毫秒） |
| `RequestTimeoutMs` | `int` | `30000` | 请求超时（毫秒） |
| `HeartbeatIntervalSec` | `int` | `30` | 心跳间隔（秒） |
| `HeartbeatTimeoutCount` | `int` | `3` | 心跳超时次数 |
| `AutoReconnect` | `bool` | `true` | 启用自动重连 |
| `ReconnectIntervalSec` | `int` | `3` | 重连间隔（秒） |
| `MaxReconnectCount` | `int` | `5` | 最大重连次数 |
| `ReceiveBufferSize` | `int` | `65536` | Socket 接收缓冲区 |
| `SendBufferSize` | `int` | `65536` | Socket 发送缓冲区 |
| `PacketBufferInitialSize` | `int` | `4096` | PacketBuffer 初始大小 |
| `PacketBufferShrinkThreshold` | `int` | `65536` | PacketBuffer 收缩阈值 |
| `LogLevel` | `GameLogLevel` | `Info` | 日志级别（Off 关闭所有日志） |
| `EnableRateLimit` | `bool` | `true` | 启用发送限流 |
| `MaxSendRate` | `int` | `100` | 每秒最大发送消息数 |
| `MaxBurstSize` | `int` | `50` | 最大突发消息数 |
| `UseWorkerThread` | `bool` | `true` | 使用工作线程（WebGL 为 false） |

### IProtocolChannel

传输通道接口，所有通道实现此接口：

```csharp
public interface IProtocolChannel : IDisposable
{
    ChannelType ChannelType { get; }
    bool IsConnected { get; }

    void OnInit();
    void Connect(string host, int port);
    void Disconnect();
    bool Send(byte[] data);

    event Action<IProtocolChannel> SendMessageEvent;
    event Action<IProtocolChannel, byte[]> ReceiveMessageEvent;
    event Action<IProtocolChannel> DisconnectServerEvent;
    event Action<IProtocolChannel, byte[], SendFailureReason> SendFailedEvent;
}
```

### RequestCommand

```csharp
// 创建请求
RequestCommand.Of(cmdMerge)                      // 空请求
RequestCommand.Of(cmdMerge, protoMessage)        // Protobuf 消息
RequestCommand.Of(cmdMerge, 123)                 // int
RequestCommand.Of(cmdMerge, "hello")             // string
RequestCommand.Of(cmdMerge, true)                // bool
RequestCommand.Of(cmdMerge, 999L)                // long
RequestCommand.Of(cmdMerge, new Vector3(1,2,3))  // Vector3
RequestCommand.Of(cmdMerge, intList)             // List<int>
RequestCommand.Of(cmdMerge, messageList)         // List<T> where T : IMessage

// 系统消息
RequestCommand.Heartbeat()                       // 心跳（自动发送）
RequestCommand.TimeSync()                        // 时间同步
```

### ResponseMessage

```csharp
// 基础访问
response.Success           // 是否成功
response.HasError          // 是否有错误
response.ErrorMessage      // 错误描述
response.ResponseStatus    // 响应状态码

// 获取数据
response.GetValue<T>()              // Protobuf 消息（带缓存）
response.GetValue(T.Parser)         // 使用 MessageParser（性能更优）
response.GetInt()                   // int
response.GetLong()                  // long
response.GetString()                // string
response.GetBool()                  // bool

// 获取列表
response.ListInt()         // List<int>
response.ListLong()        // List<long>
response.ListString()      // List<string>
response.ListBool()        // List<bool>

// Unity 类型
response.GetVector2()      // Vector2
response.GetVector3()      // Vector3
response.GetVector2Int()   // Vector2Int
response.GetVector3Int()   // Vector3Int
response.ListVector2()     // List<Vector2>
response.ListVector3()     // List<Vector3>

// 泛型（复用容器，零 GC）
response.GetList<T>(result)              // List<T>
response.GetDictionary<T>(result)        // Dictionary<int/long/string, T>
```

### CmdKit

```csharp
// 命令合并/拆分
int cmdMerge = CmdKit.Merge(1, 2);     // (1 << 16) | 2 = 65538
int cmd = CmdKit.GetCmd(cmdMerge);      // 1
int subCmd = CmdKit.GetSubCmd(cmdMerge); // 2

// 格式化
string str = CmdKit.ToString(cmdMerge); // "[1-2]"

// 命令映射（调试用）
CmdKit.MappingRequest(cmdMerge, "登录请求");
CmdKit.MappingBroadcast(cmdMerge, "聊天消息");
```

### TimeUtils

```csharp
TimeUtils.IsSynced          // 是否已同步
TimeUtils.RttMs             // 网络延迟（毫秒）
TimeUtils.ClockOffsetMs     // 时钟偏移（毫秒）
TimeUtils.ServerTimeMs      // 服务器时间戳
TimeUtils.ServerTime        // 服务器 DateTime
TimeUtils.ServerTimeUtc     // 服务器 DateTimeOffset (UTC)
TimeUtils.GetLocalTimeMs()  // 本地时间戳
```

---

## 协议规范

本节定义客户端与服务器之间的通信协议。完整协议文件：[`pisces_common.proto`](Proto/pisces_common.proto)

> 如需了解服务器端实现细节，请参阅 [服务器对接](#服务器对接) 章节。

### 核心消息

```protobuf
// 消息类型
enum MessageType {
    MESSAGE_TYPE_HEARTBEAT = 0;   // 心跳
    MESSAGE_TYPE_BUSINESS = 1;    // 业务消息
    MESSAGE_TYPE_TIME_SYNC = 2;   // 时间同步
    MESSAGE_TYPE_DISCONNECT = 3;  // 断线通知
}

// 通信协议
message ExternalMessage {
    MessageType message_type = 1;   // 消息类型
    int32 protocol_switch = 2;      // 协议开关
    int32 cmd_merge = 3;            // 业务路由（高16位 cmd，低16位 subCmd）
    int32 response_status = 4;      // 响应码: 0=成功
    string valid_msg = 5;           // 错误描述
    bytes data = 6;                 // 业务数据（Protobuf 序列化）
    int32 msg_id = 7;               // 消息 ID（请求/响应配对）
}

// 断线通知
message DisconnectNotify {
    DisconnectReason reason = 1;
    string message = 2;
    int64 estimated_recovery_time = 3;
    int64 timestamp = 4;
}

// 时间同步
message TimeSyncMessage {
    int64 client_time = 1;  // 客户端发送时间（服务器原样回传）
    int64 server_time = 2;  // 服务器时间
}
```

### 数据包格式

```
[4字节长度头 (Big-Endian)] + [ExternalMessage (Protobuf)]
```

- 最大包体：1 MB (1048576 字节)
- 长度头：不包含自身的 4 字节

### 支持的数据类型

| 类型 | Proto 消息 | C#/Unity 类型 |
|------|------------|---------------|
| 基础值 | `IntValue`, `LongValue`, `StringValue`, `BoolValue` | `int`, `long`, `string`, `bool` |
| 基础列表 | `IntValueList`, `LongValueList`, `StringValueList`, `BoolValueList` | `List<T>`, `T[]` |
| 向量 | `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int` | `UnityEngine.Vector2/3/2Int/3Int` |
| 向量列表 | `Vector2List`, `Vector3List`, ... | `List<Vector2/3/2Int/3Int>` |
| 泛型列表 | `ByteValueList` | `List<T> where T : IMessage` |
| 字典 | `IntKeyMap`, `LongKeyMap`, `StringKeyMap` | `Dictionary<K, V>` |

### 隐式转换

```csharp
// C# → Proto（自动）
IntValue score = 100;
Vector3 pos = protoVector;
List<int> ids = intValueList;

// Proto → C#（自动）
int value = intValue;
UnityEngine.Vector3 uPos = protoVec;
```

---

## 服务器对接

服务器只需导入 [`pisces_common.proto`](Proto/pisces_common.proto) 并按以下规范实现即可与客户端通信。

### 通信流程

```
┌────────────┐                          ┌────────────┐
│   Client   │                          │   Server   │
└─────┬──────┘                          └─────┬──────┘
      │                                       │
      │ ──── TCP Connect ──────────────────→  │
      │                                       │
      │ ←─── ExternalMessage (HEARTBEAT) ───  │  心跳响应
      │ ──── ExternalMessage (HEARTBEAT) ───→ │  心跳请求
      │                                       │
      │ ──── ExternalMessage (TIME_SYNC) ──→  │  时间同步请求
      │ ←─── ExternalMessage (TIME_SYNC) ───  │  时间同步响应
      │                                       │
      │ ──── ExternalMessage (BUSINESS) ───→  │  业务请求 (msg_id=1)
      │ ←─── ExternalMessage (BUSINESS) ────  │  业务响应 (msg_id=1)
      │                                       │
      │ ←─── ExternalMessage (BUSINESS) ────  │  服务器推送 (msg_id=0)
      │                                       │
      │ ←─── ExternalMessage (DISCONNECT) ──  │  断线通知
      │                                       │
```

### 服务器实现要点

#### 1. 数据包编解码

```
数据包格式: [4字节长度头 (Big-Endian)] + [ExternalMessage (Protobuf)]

伪代码:
read_packet():
    length = read_int32_big_endian()
    if length > 1MB: disconnect()  // 防止恶意大包
    data = read_bytes(length)
    return ExternalMessage.parse(data)

write_packet(msg):
    data = msg.serialize()
    write_int32_big_endian(data.length)
    write_bytes(data)
```

#### 2. 心跳处理

```
收到: message_type = HEARTBEAT
处理: 原样返回（或更新服务器时间戳）

// 服务端应记录最后心跳时间，超时未收到心跳可主动断开
```

#### 3. 时间同步处理

```
收到: message_type = TIME_SYNC
      data = TimeSyncMessage { client_time = T1 }

响应: message_type = TIME_SYNC
      data = TimeSyncMessage {
          client_time = T1,           // 原样返回
          server_time = current_ms()  // 服务器当前时间戳(毫秒)
      }
```

#### 4. 业务消息处理

```
收到: message_type = BUSINESS
      cmd_merge = 65537  // (1 << 16) | 1 = cmd=1, subCmd=1
      msg_id = 123       // 请求 ID（需在响应中原样返回）
      data = bytes       // 业务数据

路由: cmd = cmd_merge >> 16     // 高16位
      subCmd = cmd_merge & 0xFFFF  // 低16位
      handler = route(cmd, subCmd)
      result = handler.handle(data)

响应: message_type = BUSINESS
      cmd_merge = 65537       // 同请求
      msg_id = 123            // 必须与请求一致
      response_status = 0     // 0=成功, 非0=错误码
      valid_msg = ""          // 错误描述（可选）
      data = result           // 响应数据
```

#### 5. 服务器推送

```
// 服务器主动推送消息给客户端
发送: message_type = BUSINESS
      cmd_merge = 655361     // 推送路由
      msg_id = 0             // 推送消息 msg_id 固定为 0
      data = push_data       // 推送内容
```

#### 6. 断线通知

```
// 服务器需要主动断开客户端时，先发送断线通知
发送: message_type = DISCONNECT
      data = DisconnectNotify {
          reason = DUPLICATE_LOGIN,  // 断线原因
          message = "账号在其他设备登录",
          timestamp = current_ms()
      }

// 发送后关闭连接
```

### 响应状态码约定

| 状态码 | 含义 | 说明 |
|--------|------|------|
| `0` | 成功 | 请求处理成功 |
| `1` | 未知错误 | 服务器内部错误 |
| `2` | 参数错误 | 请求参数不合法 |
| `3` | 未授权 | 需要登录或权限不足 |
| `4` | 资源不存在 | 请求的资源不存在 |
| `5` | 限流 | 请求过于频繁 |

> 状态码可根据项目需求自定义，客户端通过 `response.ResponseStatus` 获取。

---

## 编辑器工具

### Project Settings

通过 **Edit → Project Settings → Pisces Client** 打开配置面板。

**功能特性：**
- **多环境管理** - 支持 Development / Staging / Production 等多套服务器环境
- **网络设置** - 传输协议、连接超时、请求超时
- **心跳设置** - 心跳间隔、超时次数
- **重连设置** - 自动重连、重连间隔、最大重连次数
- **缓冲区设置** - 接收/发送缓冲区大小
- **调试设置** - 日志开关、工作线程开关
- **自动宏管理** - 切换到 WebSocket 协议时自动添加 `ENABLE_WEBSOCKET` 宏

```csharp
// 代码中使用配置
var options = PiscesSettings.Instance.ToGameClientOptions();
PiscesSdk.Instance.Initialize(options);

// 获取当前环境
var env = PiscesSettings.Instance.ActiveEnvironment;
Debug.Log($"当前环境: {env.Name} ({env.Host}:{env.Port})");
```

### 网络监控窗口

通过 **Tools → Pisces Client → 网络监控** 打开运行时监控窗口。

**功能特性：**
- **连接状态** - 实时显示连接状态、服务器地址、连接时长
- **网络统计** - RTT、发送/接收字节数、消息速率
- **心跳状态** - 心跳间隔、上次心跳时间、超时计数
- **时间同步** - 同步状态、服务器时间、RTT
- **消息日志** - 实时消息收发日志（支持过滤）

### 菜单项

| 菜单路径 | 功能 |
|----------|------|
| **Tools → Pisces Client → 打开设置** | 打开 Project Settings |
| **Tools → Pisces Client → 网络监控** | 打开网络监控窗口 |
| **Tools → Pisces Client → 定位配置文件** | 在 Project 窗口中定位配置资源 |
| **Tools → Pisces Client → 恢复默认设置** | 重置所有配置为默认值 |
| **Tools → Pisces Client → 文档** | 打开在线文档 |
| **Tools → Pisces Client → 关于** | 显示版本信息 |

---

## 平台适配

| 平台 | TCP | UDP | WebSocket | 推荐 |
|------|-----|-----|-----------|------|
| Windows/macOS/Linux | ✅ | ✅ | ✅ | TCP |
| Android/iOS | ✅ | ✅ | ✅ | TCP |
| **WebGL** | ❌ | ❌ | ✅ | **WebSocket** |

### 通道特性

| 通道 | 特性 |
|------|------|
| **TcpChannel** | 可靠有序传输，支持 DNS 解析，支持 IPv4/IPv6 |
| **UdpChannel** | 低延迟无连接，支持 DNS 解析，支持 IPv4/IPv6 |
| **WebSocketChannel** | WebGL 兼容，支持 `ws://` 和 `wss://` 协议 |

### 启用 WebSocket

WebSocket 功能通过 `ENABLE_WEBSOCKET` 编译符号控制。

**方式 1：自动管理（推荐）**

在 **Edit → Project Settings → Pisces Client** 中切换协议为 WebSocket，编辑器会自动添加宏。

**方式 2：手动添加**

1. 打开 **Edit → Project Settings → Player**
2. 找到 **Scripting Define Symbols**
3. 添加 `ENABLE_WEBSOCKET`

```csharp
var options = new GameClientOptions
{
    ChannelType = ChannelType.WebSocket,
    Host = "wss://game.server.com",
    Port = 443
};
```

> 使用 TCP/UDP 时，移除 `ENABLE_WEBSOCKET` 可减小包体。

---

## 常见问题

**Q: 如何处理断线重连？**
```csharp
PiscesSdk.Instance.OnStateChanged += state =>
{
    if (state == ConnectionState.Connected)
    {
        // 重连成功，同步数据
        SyncGameData();
    }
};
```

**Q: 如何取消请求？**
```csharp
var cts = new CancellationTokenSource();
var task = PiscesSdk.Instance.RequestAsync<T>(cmd, cts.Token);

// 取消
cts.Cancel();
```

**Q: 重复调用 ConnectAsync 会发生什么？**
```csharp
// 已连接到相同地址 → 跳过，输出日志
await sdk.ConnectAsync("127.0.0.1", 10100);
await sdk.ConnectAsync("127.0.0.1", 10100); // 跳过

// 已连接到不同地址 → 断开旧连接，重建客户端
await sdk.ConnectAsync("192.168.1.100", 10100); // 重连
```

**Q: WebGL 连接失败？**
- 确保使用 `ChannelType.WebSocket`
- Host 使用完整 URL（`ws://` 或 `wss://`）
- 检查服务器 CORS 配置
- 确保已添加 `ENABLE_WEBSOCKET` 宏

**Q: 如何自定义日志？**
```csharp
// 关闭所有日志
GameLogger.Level = GameLogLevel.Off;

// 只输出 Warning 及以上
GameLogger.Level = GameLogLevel.Warning;

// 开发调试时查看所有细节
GameLogger.Level = GameLogLevel.Verbose;

// 自定义日志实现
GameLogger.SetLog(new MyCustomLogger());
```

**日志级别说明：**

| 级别 | 说明 | 使用场景 |
|------|------|----------|
| `GameLogLevel.Verbose` | 消息收发细节、序列化数据 | 深度调试 |
| `GameLogLevel.Debug` | 状态变化、连接事件 | 开发调试 |
| `GameLogLevel.Info` | 连接成功、断开等关键节点（默认） | 日常开发 |
| `GameLogLevel.Warning` | 重连、超时等 | 测试环境 |
| `GameLogLevel.Error` | 异常、失败 | 生产环境 |
| `GameLogLevel.Off` | 关闭所有日志 | 性能测试 |

**Q: 如何获取服务器时间？**
```csharp
// 先请求同步
PiscesSdk.Instance.RequestTimeSync();

// 然后使用
if (TimeUtils.IsSynced)
{
    var serverTime = TimeUtils.ServerTime;
    var rtt = TimeUtils.RttMs;
}
```

---

## 目录结构

```
Pisces.Client.Unity/
├── Editor/                             # 编辑器工具
│   ├── PiscesMenuItems.cs              # 菜单项
│   ├── PiscesNetworkMonitor.cs         # 网络监控窗口
│   └── Settings/
│       └── PiscesSettingsProvider.cs   # Project Settings 面板（含自动宏管理）
├── Plugins/                            # 第三方插件
│   └── Google.Protobuf/                # Protobuf 运行时库
│       ├── Google.Protobuf.dll         # Protobuf 核心库
│       ├── Google.Protobuf.xml         # XML 文档
│       ├── System.Buffers.dll          # 缓冲区支持
│       ├── System.Memory.dll           # 内存操作支持
│       └── System.Runtime.CompilerServices.Unsafe.dll  # 底层内存操作
├── Proto/
│   └── pisces_common.proto             # 协议定义
├── Runtime/
│   ├── Assembly/                       # 程序集信息
│   │   └── AssemblyInfo.cs             # 程序集元数据
│   ├── Network/
│   │   ├── Channel/                    # 传输通道
│   │   │   ├── IProtocolChannel.cs     # 通道接口（含 IDisposable）
│   │   │   ├── ChannelFactory.cs       # 通道工厂
│   │   │   ├── ChannelType.cs          # 通道类型枚举
│   │   │   ├── ProtocolChannelBase.cs  # 通道基类（TCP/UDP，支持 DNS/IPv6）
│   │   │   ├── TcpChannel.cs           # TCP 通道（返回原始字节）
│   │   │   ├── UdpChannel.cs           # UDP 通道（支持 DNS/IPv6）
│   │   │   └── WebSocketChannel.cs     # WebSocket 通道（需 ENABLE_WEBSOCKET）
│   │   ├── Client/                     # 客户端实现
│   │   │   ├── GameClient.cs           # 网络客户端核心（partial）
│   │   │   ├── GameClient.Messaging.cs # 消息收发模块
│   │   │   ├── GameClient.Heartbeat.cs # 心跳保活模块
│   │   │   ├── GameClient.Reconnect.cs # 自动重连模块
│   │   │   ├── GameClient.PendingRequests.cs  # 待处理请求管理
│   │   │   ├── GameClientOptions.cs    # 配置项
│   │   │   └── IGameClient.cs          # 客户端接口
│   │   └── Core/                       # 核心组件
│   │       ├── ConnectionState.cs      # 连接状态枚举
│   │       ├── ConnectionStateMachine.cs # 连接状态机
│   │       ├── NetworkStatistics.cs    # 网络统计
│   │       ├── PacketBuffer.cs         # 智能粘包处理（自动扩缩容）
│   │       ├── PacketCodec.cs          # 编解码器
│   │       └── SendResult.cs           # 发送结果枚举
│   ├── Protocol/                       # 协议相关
│   │   ├── CmdKit.cs                   # 命令合并/拆分工具
│   │   ├── CollectionConvertHelper.cs  # 集合类型转换助手
│   │   ├── PiscesCommon.cs             # 生成的 Protobuf 类
│   │   ├── PiscesCommonExtensions.cs   # Protobuf 类型扩展方法
│   │   └── ProtoSerializer.cs          # Protobuf 序列化工具
│   ├── Sdk/                            # SDK 层
│   │   ├── PiscesSdk.cs                # SDK 入口（Facade）
│   │   ├── PiscesException.cs          # SDK 异常定义
│   │   ├── MsgIdManager.cs             # 消息 ID 管理
│   │   ├── RequestCommand.cs           # 请求命令
│   │   ├── RequestCommand.Factory.cs   # 请求命令工厂方法
│   │   ├── ResponseMessage.cs          # 响应消息
│   │   ├── ResponseMessage.Accessors.cs # 响应消息数据访问器
│   │   └── Managers/                   # 内部管理器
│   │       ├── ConnectionManager.cs    # 连接管理（智能复用）
│   │       ├── MessageRouter.cs        # 消息路由（异常隔离）
│   │       └── RequestManager.cs       # 请求管理
│   ├── Settings/                       # 配置系统
│   │   ├── PiscesSettings.cs           # 全局配置 ScriptableObject
│   │   └── SettingsPaths.cs            # 配置文件路径常量
│   ├── Unity/                          # Unity 集成
│   │   └── PiscesLifecycleManager.cs   # Unity 生命周期管理
│   └── Utils/                          # 工具类
│       ├── MainThreadDispatcher.cs     # 主线程调度器
│       ├── RateLimiter.cs              # 令牌桶限流器
│       ├── TimeUtils.cs                # 时间同步工具
│       ├── Log/                        # 日志系统
│       │   ├── DefaultLog.cs           # 默认日志实现
│       │   ├── GameLogger.cs           # 游戏日志门面
│       │   ├── GameLogLevel.cs         # 日志级别枚举
│       │   └── ILog.cs                 # 日志接口
│       └── Pool/                       # 对象池
│           ├── IPoolable.cs            # 可池化对象接口
│           └── ReferencePool.cs        # 引用对象池
├── LICENSE                             # MIT 许可证
├── package.json
└── README.md
```

---

## License

MIT License
