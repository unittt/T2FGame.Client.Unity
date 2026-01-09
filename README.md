# Pisces Client SDK

<div align="center">

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**高性能、模块化、跨平台的 Unity 游戏客户端网络 SDK**

</div>

---

## 📖 项目简介

Pisces Client SDK 是一个专为 Unity 游戏开发设计的**轻量、高性能**的网络通信框架。基于 **Protobuf 协议**，提供完整的客户端网络功能。

| 特性 | 说明 |
|------|------|
| 🎯 解耦 | 与游戏业务无耦合，可集成到任意 Unity 项目 |
| ⚡ 高性能 | 基于 UniTask 异步编程，对象池减少 GC |
| 🌐 跨平台 | TCP、UDP、WebSocket，适配所有平台 |
| 🛡️ 可靠性 | 自动重连、心跳保活、完善错误处理 |

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
| [**UnityWebSocket**](https://github.com/psygames/UnityWebSocket) | latest | WebGL 平台必需 |

---

## 📝 协议规范

服务器只需适配 [`pisces_common.proto`](Proto/pisces_common.proto) 即可与客户端通信。

### 核心通信协议

```protobuf
message ExternalMessage {
    int32 cmd_code = 1;           // 请求命令类型
    int32 protocol_switch = 2;    // 协议开关
    int32 cmd_merge = 3;          // 业务路由（高16位 cmd，低16位 subCmd）
    int32 response_status = 4;    // 响应码: 0=成功
    string valid_msg = 5;         // 错误描述
    bytes data = 6;               // 业务数据（Protobuf 序列化）
    int32 msg_id = 7;             // 消息 ID（请求/响应配对）
    map<string, string> metadata = 8;
}
```

### 支持的数据类型

| 类型 | Proto 消息 | 说明 |
|------|------------|------|
| 基础值 | `IntValue`, `LongValue`, `StringValue`, `BoolValue` | 单值包装 |
| 基础列表 | `IntValueList`, `LongValueList`, `StringValueList`, `BoolValueList` | 列表包装 |
| 泛型列表 | `ByteValueList` | 任意 Protobuf 对象列表 |
| 向量 | `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int` | Unity 向量 |
| 向量列表 | `Vector2List`, `Vector3List`, `Vector2IntList`, `Vector3IntList` | 向量列表 |
| 字典 | `IntKeyMap`, `LongKeyMap`, `StringKeyMap`, `ByteValueMap` | 键值对映射 |

---

## 🚀 快速开始

### 1. 初始化与连接

```csharp
var options = new GameClientOptions
{
    Host = "127.0.0.1",
    Port = 10100,
    AutoReconnect = true
};

PiscesSdk.Instance.Initialize(options);
PiscesSdk.Instance.OnStateChanged += state => Debug.Log($"状态: {state}");

await PiscesSdk.Instance.ConnectAsync();
```

### 2. 发送请求

```csharp
// 异步请求
var response = await PiscesSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(1001, request);

// 回调模式
PiscesSdk.Instance.Send<LoginRequest, LoginResponse>(1001, request, resp => {
    Debug.Log($"登录成功: {resp.UserId}");
});

// 仅发送
PiscesSdk.Instance.Send(2001, chatMsg);
```

### 3. 订阅推送

```csharp
Action<ChatMessage> handler = msg => Debug.Log(msg.Content);
PiscesSdk.Instance.Subscribe(3001, handler);

// 取消订阅
PiscesSdk.Instance.Unsubscribe(3001, handler);
```

---

## 📚 API 概览

### PiscesSdk 主要方法

| 方法 | 说明 |
|------|------|
| `Initialize(options)` | 初始化 SDK |
| `ConnectAsync()` | 连接服务器 |
| `RequestAsync<TReq, TResp>(cmd, req)` | 异步请求 |
| `Send(cmd, msg)` | 仅发送消息 |
| `Send<TReq, TResp>(cmd, req, callback)` | 回调模式 |
| `Subscribe<T>(cmd, handler)` | 订阅推送 |
| `Unsubscribe<T>(cmd, handler)` | 取消订阅 |
| `Close()` | 关闭连接 |

### ResponseMessage 访问器

| 方法 | 说明 |
|------|------|
| `GetValue<T>()` | 获取 Protobuf 消息（带缓存） |
| `GetInt/Long/String/Bool()` | 基础类型 |
| `ListInt/Long/String/Bool()` | 基础列表 |
| `GetVector2/3/2Int/3Int()` | Unity Vector |
| `ListVector2/3/2Int/3Int()` | Vector 列表 |
| `GetList<T>(result)` | 泛型列表（复用容器） |
| `GetDictionary<T>(result)` | 字典（复用容器） |

### GameClientOptions 配置

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `ChannelType` | Tcp | TCP/UDP/WebSocket |
| `Host` | localhost | 服务器地址 |
| `Port` | 9090 | 服务器端口 |
| `ConnectTimeoutMs` | 10000 | 连接超时 |
| `RequestTimeoutMs` | 30000 | 请求超时 |
| `HeartbeatIntervalSec` | 30 | 心跳间隔 |
| `AutoReconnect` | true | 自动重连 |
| `MaxReconnectCount` | 5 | 最大重连次数 |

---

## 🔄 类型转换

### 隐式转换（自动）

| Protobuf 类型 | C#/Unity 类型 |
|---------------|---------------|
| `IntValue` | `int` |
| `LongValue` | `long` |
| `StringValue` | `string` |
| `BoolValue` | `bool` |
| `IntValueList` | `List<int>` / `int[]` |
| `LongValueList` | `List<long>` / `long[]` |
| `StringValueList` | `List<string>` / `string[]` |
| `BoolValueList` | `List<bool>` / `bool[]` |
| `Vector2/3` | `UnityEngine.Vector2/3` |
| `Vector2Int/3Int` | `UnityEngine.Vector2Int/3Int` |
| `Vector2/3List` | `List<UnityEngine.Vector2/3>` |
| `Vector2Int/3IntList` | `List<UnityEngine.Vector2Int/3Int>` |

```csharp
// 隐式转换示例
IntValue score = 100;           // int → IntValue
Vector3 pos = protoVector;      // Proto.Vector3 → UnityEngine.Vector3
```

### From 方法（泛型类型）

```csharp
// 泛型列表
var list = ByteValueList.From(enemyList);
list.ToList(result);  // 反向转换

// 字典
var map = IntKeyMap.From(itemDict);
map.ToDictionary(result);  // 反向转换
```

支持类型：`IntKeyMap`、`LongKeyMap`、`StringKeyMap`、`ByteValueMap`

---

## 🔧 平台适配

| 平台 | TCP | UDP | WebSocket | 推荐 |
|------|-----|-----|-----------|------|
| Windows/macOS/Linux | ✅ | ✅ | ✅ | TCP |
| Android/iOS | ✅ | ✅ | ✅ | TCP |
| **WebGL** | ❌ | ❌ | ✅ | **WebSocket** |

```csharp
#if UNITY_WEBGL
var options = new GameClientOptions
{
    ChannelType = ChannelType.WebSocket,
    Host = "wss://game.server.com"
};
#endif
```

---

## ❓ 常见问题

**Q: 如何处理断线重连？**
```csharp
PiscesSdk.Instance.OnStateChanged += state => {
    if (state == ConnectionState.Connected) ReLoginOrSyncData();
};
```

**Q: 如何取消请求？**
```csharp
var cts = new CancellationTokenSource();
await PiscesSdk.Instance.RequestAsync<T>(cmd, cts.Token);
cts.Cancel();
```

**Q: WebGL 连接失败？**
- 确保使用 `ChannelType.WebSocket`
- Host 使用完整 URL（`ws://` 或 `wss://`）

---

## 📄 License

MIT License
