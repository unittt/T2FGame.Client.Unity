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
- [API 参考](#-api-参考)
- [协议规范](#-协议规范)
- [平台适配](#-平台适配)
- [常见问题](#-常见问题)

---

## 📖 项目简介

Pisces Client SDK 是一个专为 Unity 游戏开发设计的**轻量、高性能**的网络通信框架。基于 **Protobuf 协议**，提供完整的客户端网络功能。

| 特性 | 说明 |
|------|------|
| 🎯 **零业务耦合** | 纯网络层，可集成到任意 Unity 项目 |
| ⚡ **高性能** | 基于 UniTask 异步编程，对象池减少 GC |
| 🌐 **跨平台** | TCP、UDP、WebSocket，适配所有平台 |
| 🛡️ **可靠性** | 自动重连、心跳保活、断线通知 |
| ⏱️ **时间同步** | 客户端与服务器时钟同步，RTT 测量 |
| 🔄 **Unity 集成** | 自动生命周期管理，编辑器友好 |

---

## 🏗️ 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  业务层 (Game Logic)                                     │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  PiscesSdk (Facade)                                     │
│  ├─ ConnectionManager   (连接生命周期)                   │
│  ├─ MessageRouter       (消息订阅/分发)                  │
│  └─ RequestManager      (请求/响应处理)                  │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  GameClient (网络核心)                                   │
│  ├─ 连接状态管理                                         │
│  ├─ 心跳保活 & 自动重连                                  │
│  ├─ 时间同步 & RTT 测量                                  │
│  └─ 断线通知处理                                         │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  Protocol Channel (传输层)                               │
│  ├─ TcpChannel      (可靠、有序)                         │
│  ├─ UdpChannel      (低延迟)                             │
│  └─ WebSocketChannel (WebGL 兼容)                        │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│  PacketCodec (协议编解码)                                │
│  [4字节长度头] + [ExternalMessage (Protobuf)]            │
└─────────────────────────────────────────────────────────┘
```

---

## 📦 安装

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

## 🚀 快速开始

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

## 🔧 核心功能

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

## 📚 API 参考

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
| `Host` | `string` | `localhost` | 服务器地址 |
| `Port` | `int` | `9090` | 服务器端口 |
| `ConnectTimeoutMs` | `int` | `10000` | 连接超时（毫秒） |
| `RequestTimeoutMs` | `int` | `30000` | 请求超时（毫秒） |
| `HeartbeatIntervalSec` | `int` | `30` | 心跳间隔（秒） |
| `HeartbeatTimeoutCount` | `int` | `3` | 心跳超时次数 |
| `AutoReconnect` | `bool` | `true` | 启用自动重连 |
| `ReconnectIntervalSec` | `int` | `3` | 重连间隔（秒） |
| `MaxReconnectCount` | `int` | `5` | 最大重连次数 |
| `ReceiveBufferSize` | `int` | `65536` | 接收缓冲区 |
| `SendBufferSize` | `int` | `65536` | 发送缓冲区 |
| `EnableLog` | `bool` | `true` | 启用日志 |

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

## 📝 协议规范

服务器只需适配 [`pisces_common.proto`](Proto/pisces_common.proto) 即可与客户端通信。

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

## 🔧 平台适配

| 平台 | TCP | UDP | WebSocket | 推荐 |
|------|-----|-----|-----------|------|
| Windows/macOS/Linux | ✅ | ✅ | ✅ | TCP |
| Android/iOS | ✅ | ✅ | ✅ | TCP |
| **WebGL** | ❌ | ❌ | ✅ | **WebSocket** |

### 启用 WebSocket

WebSocket 功能通过 `ENABLE_WEBSOCKET` 编译符号控制。

**启用步骤：**
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

> 💡 使用 TCP/UDP 时，移除 `ENABLE_WEBSOCKET` 可减小包体。

---

## ❓ 常见问题

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

**Q: WebGL 连接失败？**
- 确保使用 `ChannelType.WebSocket`
- Host 使用完整 URL（`ws://` 或 `wss://`）
- 检查服务器 CORS 配置

**Q: Unity 编辑器退出时报错？**

SDK 已自动处理 Unity 生命周期。`PiscesLifecycleManager` 会在退出 Play Mode 时自动清理资源。

**Q: 如何自定义日志？**
```csharp
// 禁用日志
GameLogger.Enabled = false;

// 或实现自定义 ILog
GameLogger.Logger = new MyCustomLogger();
```

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

## 📁 目录结构

```
Pisces.Client.Unity/
├── Proto/
│   └── pisces_common.proto       # 协议定义
├── Runtime/
│   ├── Network/
│   │   ├── Channel/              # 传输通道（TCP/UDP/WebSocket）
│   │   ├── GameClient.cs         # 网络客户端核心
│   │   ├── GameClientOptions.cs  # 配置项
│   │   ├── PacketCodec.cs        # 编解码器
│   │   └── PacketBuffer.cs       # 粘包处理
│   ├── Protocol/
│   │   ├── PiscesCommon.cs       # 生成的 Protobuf 类
│   │   ├── CmdKit.cs             # 命令路由工具
│   │   └── ProtoSerializer.cs    # 序列化辅助
│   ├── Sdk/
│   │   ├── PiscesSdk.cs          # SDK 入口（Facade）
│   │   ├── RequestCommand.cs     # 请求命令
│   │   ├── ResponseMessage.cs    # 响应消息
│   │   └── Managers/             # 内部管理器
│   ├── Utils/
│   │   ├── TimeUtils.cs          # 时间同步工具
│   │   ├── Log/                  # 日志系统
│   │   └── Pool/                 # 对象池
│   └── Unity/
│       └── PiscesLifecycleManager.cs  # Unity 生命周期
├── package.json
└── README.md
```

---

## 📄 License

MIT License
