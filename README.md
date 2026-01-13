# Pisces Client SDK

<div align="center">

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**高性能 Unity 游戏客户端网络 SDK** · 基于 UniTask + Protobuf

[快速开始](#快速开始) · [API 参考](#api-参考) · [服务器对接](#服务器对接)

</div>

---

## 项目简介

Pisces Client SDK 是**纯客户端网络通信框架**，专为"Unity 客户端 + 独立服务器"架构设计。

| 特性 | 说明 |
|------|------|
| 多协议支持 | TCP / UDP / WebSocket |
| 现代异步 | UniTask async/await |
| 开箱即用 | 心跳保活、自动重连、时间同步、流量控制 |
| 零业务耦合 | 纯网络层，不侵入业务代码 |

### 适用场景

| ✅ 适合 | ❌ 不适合 |
|--------|----------|
| 已有 Java/Go/C++ 服务器团队 | 无服务器开发经验 |
| 卡牌/回合制/SLG/MMO | FPS/MOBA 需要预测回滚 |
| 消息驱动架构 | 需要 NetworkTransform 自动同步 |
| WebGL 平台 | 需要快速原型验证 |

---

## 安装

```
https://github.com/PiscesGameDev/Pisces.Client.Unity.git
```

**依赖**: [UniTask](https://github.com/Cysharp/UniTask) (必需) · [Protobuf](https://github.com/protocolbuffers/protobuf) (必需) · [UnityWebSocket](https://github.com/psygames/UnityWebSocket) (WebGL 可选)

---

## 快速开始

### 初始化与连接

```csharp
// 初始化
PiscesSdk.Instance.Initialize(new GameClientOptions
{
    Host = "127.0.0.1",
    Port = 10100,
    AutoReconnect = true
});

// 事件订阅
PiscesSdk.Instance.OnStateChanged += state => Debug.Log($"状态: {state}");
PiscesSdk.Instance.OnError += ex => Debug.LogError(ex.Message);

// 连接
await PiscesSdk.Instance.ConnectAsync();
```

### 发送请求

```csharp
// 异步请求（推荐）
var response = await PiscesSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
    CmdKit.Merge(1, 1), new LoginRequest { Username = "player1" }
);

// 仅发送
PiscesSdk.Instance.Send(CmdKit.Merge(2, 1), chatMessage);
```

### 订阅消息

```csharp
// 订阅服务器推送
IDisposable sub = PiscesSdk.Instance.Subscribe<ChatMessage>(
    CmdKit.Merge(10, 1),
    msg => Debug.Log(msg.Content)
);

// 取消订阅
sub.Dispose();
```

### 关闭连接

```csharp
PiscesSdk.Instance.Close();    // 关闭（禁止重连）
PiscesSdk.Instance.Dispose();  // 释放资源
```

---

## 核心功能

### 连接状态

```csharp
// 状态: Disconnected → Connecting → Connected ⇄ Reconnecting → Closed
PiscesSdk.Instance.OnStateChanged += state => { };
```

### 断线通知

```csharp
PiscesSdk.Instance.OnDisconnectNotify += notify =>
{
    // notify.Reason: DuplicateLogin, Banned, ServerMaintenance, NetworkError...
    // notify.Message: 断线原因描述
};
```

| 断线原因 | 自动重连 |
|---------|---------|
| DuplicateLogin / Banned / ServerMaintenance | ❌ |
| NetworkError / IdleTimeout | ✅ |

### 时间同步

```csharp
PiscesSdk.Instance.RequestTimeSync();

if (PiscesSdk.Instance.IsTimeSynced)
{
    DateTime serverTime = PiscesSdk.Instance.ServerTime;
    float rtt = PiscesSdk.Instance.RttMs;
}
```

### 配置选项

```csharp
new GameClientOptions
{
    ChannelType = ChannelType.Tcp,      // 协议类型
    ConnectTimeoutMs = 10000,           // 连接超时
    RequestTimeoutMs = 30000,           // 请求超时
    HeartbeatIntervalSec = 30,          // 心跳间隔
    HeartbeatTimeoutCount = 3,          // 心跳超时次数
    AutoReconnect = true,               // 自动重连
    MaxReconnectCount = 5,              // 最大重连次数
    EnableRateLimit = true,             // 启用限流
    MaxSendRate = 100,                  // 消息/秒
    LogLevel = GameLogLevel.Info        // 日志级别
};
```

---

## API 参考

### PiscesSdk 主要方法

| 方法 | 说明 |
|------|------|
| `Initialize(options)` | 初始化 SDK |
| `ConnectAsync()` | 异步连接 |
| `DisconnectAsync()` | 断开连接（允许重连） |
| `Close()` | 关闭（禁止重连） |
| `Dispose()` | 释放资源 |
| `RequestAsync<TReq, TRes>(cmd, req)` | 异步请求 |
| `Send(cmd, message)` | 发送消息 |
| `Subscribe<T>(cmd, callback)` | 订阅消息 |
| `RequestTimeSync()` | 请求时间同步 |

### PiscesSdk 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `State` | ConnectionState | 当前连接状态 |
| `IsConnected` | bool | 是否已连接 |
| `IsTimeSynced` | bool | 是否已同步时间 |
| `ServerTime` | DateTime | 服务器时间 |
| `RttMs` | float | 网络延迟(ms) |

### PiscesSdk 事件

| 事件 | 说明 |
|------|------|
| `OnStateChanged` | 连接状态变化 |
| `OnDisconnectNotify` | 收到断线通知 |
| `OnError` | 发生错误 |
| `OnSendFailed` | 发送失败 |

---

## 协议规范

### 消息格式

```
┌─────────────────┬─────────────────────────────────┐
│  Length (4B)    │  ExternalMessage (Protobuf)     │
│  小端序 uint32   │  包含 cmdMerge, msgId, data     │
└─────────────────┴─────────────────────────────────┘
```

### CmdMerge 路由

```csharp
// 高16位 cmd + 低16位 subCmd = 32位路由标识
int cmdMerge = CmdKit.Merge(1, 2);        // cmd=1, subCmd=2 → 0x00010002
(int cmd, int sub) = CmdKit.Split(cmdMerge);
string str = CmdKit.ToString(cmdMerge);    // "1.2"
```

### ExternalMessage 结构

```protobuf
message ExternalMessage {
    int32 cmdMerge = 1;       // 路由标识
    int32 responseStatus = 2; // 响应状态码
    string validMsg = 3;      // 错误消息
    bytes data = 4;           // 业务数据
    int32 msgId = 5;          // 消息ID
}
```

---

## 服务器对接

### 消息类型

服务器需根据 `ExternalMessage.messageType` 处理不同类型的消息：

| MessageType | 值 | 说明 |
|-------------|---|------|
| Heartbeat | 0 | 心跳消息，服务器需原样返回 |
| Business | 1 | 业务消息，根据 `cmdMerge` 路由 |
| TimeSync | 2 | 时间同步，返回 `TimeSyncMessage` |
| Disconnect | 3 | 断线通知，服务器主动断开时发送 |

### 协议实现要点

1. **心跳**: 收到 `messageType=0` 时，原样返回该消息
2. **时间同步**: 收到 `messageType=2` 时，返回 `TimeSyncMessage { clientTime, serverTime }`
3. **断线通知**: 主动断开客户端前，发送 `messageType=3` + `DisconnectNotify`
4. **业务消息**: `messageType=1`，根据 `cmdMerge` 分发到对应处理器

---

## 平台适配

| 平台 | 协议 | 说明 |
|------|------|------|
| Windows/Mac/Linux | TCP/UDP | 完整支持 |
| Android/iOS | TCP/UDP | 完整支持 |
| WebGL | WebSocket | 需启用 `ENABLE_WEBSOCKET` 宏 |

### WebGL 配置

```csharp
// 1. Player Settings → Scripting Define Symbols 添加: ENABLE_WEBSOCKET
// 2. 使用 WebSocket 连接
var options = new GameClientOptions
{
    ChannelType = ChannelType.WebSocket,
    Host = "wss://game.example.com"
};
```

---

## 编辑器工具

- **Project Settings → Pisces**: 可视化配置服务器环境、网络参数
- **Window → Pisces → Network Monitor**: 实时监控连接状态、消息统计

---

## 常见问题

**Q: 与 Mirror/Photon 有什么区别？**
> Pisces 是纯客户端 SDK，需要自建服务器。Mirror/Photon 是全栈方案，包含服务器和客户端。

**Q: 支持哪些服务器语言？**
> 任何支持 Protobuf 和 TCP/WebSocket 的语言：Java、Go、C++、Rust、C# 等。

**Q: WebGL 为什么要单独配置？**
> 浏览器不支持原生 Socket，必须使用 WebSocket。

---

## License

MIT License - 详见 [LICENSE](LICENSE)
