# T2FGame Client SDK

<div align="center">

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**高性能、模块化、跨平台的 Unity 游戏客户端网络 SDK**

[特性](#-核心特性) • [安装](#-安装) • [快速开始](#-快速开始) • [文档](#-api-文档) • [示例](#-完整示例)

</div>

---

## 📖 项目简介

T2FGame Client SDK 是一个专为 Unity 游戏开发设计的**独立、轻量、高性能**的网络通信框架。它基于 **ioGame 协议**，提供完整的客户端网络功能，包括连接管理、消息收发、心跳保活、自动重连等。

### 设计理念

- **🎯 独立性**：零业务依赖，不依赖任何游戏框架，可在任意 Unity 项目中使用
- **🏗️ 模块化**：采用三层管理器架构（连接、路由、请求分离），职责清晰，易于维护
- **⚡ 高性能**：基于 UniTask 的零 GC 异步编程，multicast delegate 高效订阅机制
- **🌐 跨平台**：支持 TCP、UDP、WebSocket，适配桌面、移动、WebGL 等所有平台
- **🔧 易用性**：简洁的 API 设计，符合 C#/.NET 最佳实践
- **🛡️ 可靠性**：完善的错误处理、自动重连、心跳保活机制

---

## ✨ 核心特性

### 网络通信
- ✅ **多协议支持**：TCP、UDP、WebSocket（自动适配平台）
- ✅ **Protobuf 序列化**：基于 ioGame 协议的高效序列化
- ✅ **请求-响应模型**：支持 async/await 异步请求，自动匹配响应
- ✅ **回调模式**：支持 Send<TRequest, TResponse>(callback) 回调式请求
- ✅ **服务器推送**：支持 cmdMerge 消息订阅和自动分发

### 模块化架构
- ✅ **ConnectionManager**：专注连接管理、状态监控、自动重连
- ✅ **MessageRouter**：高性能消息路由，支持泛型自动解包
- ✅ **RequestManager**：统一请求管理，支持三种请求模式

### 连接管理
- ✅ **自动重连**：可配置的指数退避重连策略
- ✅ **心跳保活**：自动心跳检测，及时发现连接断开
- ✅ **连接状态管理**：完整的状态机（Disconnected → Connecting → Connected → Reconnecting）
- ✅ **超时控制**：连接超时、请求超时可配置

### 高级特性
- ✅ **线程安全**：ConcurrentDictionary 保证并发安全
- ✅ **对象池**：减少 GC 压力，提升性能
- ✅ **TCP 粘包处理**：完整的消息帧解析
- ✅ **WebGL 支持**：自动检测平台，禁用线程以适配 WebGL
- ✅ **灵活配置**：20+ 可配置参数，满足各种需求

---

## 📦 安装

### 方式 1：Git URL（推荐）

1. 打开 Unity Package Manager
2. 点击 `+` → `Add package from git URL...`
3. 输入：
```
https://github.com/your-repo/T2FGame.Client.Unity.git
```

### 方式 2：本地安装

1. 下载本仓库到本地
2. 打开 Unity Package Manager
3. 点击 `+` → `Add package from disk...`
4. 选择 `package.json` 文件

### 方式 3：直接复制

将整个 `T2FGame.Client.Unity` 文件夹复制到项目的 `Assets` 目录下。

### 依赖项

> **⚠️ 重要**：本 SDK 依赖以下包，请确保已安装：

| 依赖 | 版本 | 说明 |
|------|------|------|
| **UniTask** | 2.3.3+ | 异步编程框架（必需） |
| **Protobuf** | 3.x | 消息序列化（必需） |
| **UnityWebSocket** | latest | WebSocket 通信支持（WebGL 平台必需） |

**安装依赖包**：

1. 打开 Unity Package Manager
2. 点击 `+` → `Add package from git URL...`
3. 依次添加以下 URL：

```
# UniTask（异步编程框架）
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

# UnityWebSocket（WebSocket 支持）
https://github.com/psygames/UnityWebSocket.git#upm
```

> **💡 提示**：如果你的项目只在桌面/移动平台运行（不需要 WebGL），UnityWebSocket 依赖是可选的。但为了跨平台兼容性，建议安装。

---

## 🚀 快速开始

### 1. 基础用法

```csharp
using Cysharp.Threading.Tasks;
using T2FGame.Client.Network;
using T2FGame.Client.Sdk;
using UnityEngine;

public class NetworkExample : MonoBehaviour
{
    private async void Start()
    {
        // 1. 配置客户端
        var options = new GameClientOptions
        {
            Host = "127.0.0.1",
            Port = 10100,
            AutoReconnect = true,
            EnableLog = true
        };

        // 2. 初始化 SDK
        T2FGameSdk.Instance.Initialize(options);

        // 3. 订阅事件
        T2FGameSdk.Instance.OnStateChanged += OnConnectionStateChanged;

        // 4. 连接服务器
        try
        {
            await T2FGameSdk.Instance.ConnectAsync();
            Debug.Log("连接成功！");
        }
        catch (Exception ex)
        {
            Debug.LogError($"连接失败：{ex.Message}");
        }
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        Debug.Log($"连接状态变化：{state}");
    }
}
```

### 2. 发送请求并等待响应

```csharp
using T2FGame.Protocol; // 你的 Protobuf 消息定义

public class LoginExample : MonoBehaviour
{
    private async UniTask Login()
    {
        // 创建请求消息
        var request = new LoginRequest
        {
            Username = "player123",
            Password = "password"
        };

        try
        {
            // 发送请求并等待响应（带泛型参数）
            var response = await T2FGameSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
                cmdMerge: 1001, // 命令码（根据服务器协议定义）
                request: request
            );

            Debug.Log($"登录成功！UserId: {response.UserId}");
        }
        catch (TimeoutException)
        {
            Debug.LogError("请求超时");
        }
        catch (Exception ex)
        {
            Debug.LogError($"登录失败：{ex.Message}");
        }
    }
}
```

### 3. 仅发送消息（不等待响应）

```csharp
public void SendHeartbeat()
{
    // 发送心跳消息（不需要等待响应）
    T2FGameSdk.Instance.Send(cmdMerge: 1);
}

public void SendChatMessage(string message)
{
    var chatMsg = new ChatMessage { Content = message };
    T2FGameSdk.Instance.Send(cmdMerge: 2001, chatMsg);
}
```

### 4. 订阅服务器推送消息

```csharp
private void Start()
{
    // 方式 1: 订阅并自动解包为指定类型（推荐）
    int chatCmdMerge = CmdKit.GetMergeCmd(2, 1);
    T2FGameSdk.Instance.Subscribe<ChatMessage>(chatCmdMerge, OnChatMessage);

    // 方式 2: 订阅原始消息
    T2FGameSdk.Instance.Subscribe(chatCmdMerge, message =>
    {
        var chatMsg = ProtoSerializer.Deserialize<ChatMessage>(message.Data);
        Debug.Log($"收到聊天: {chatMsg.Content}");
    });
}

private void OnChatMessage(ChatMessage msg)
{
    Debug.Log($"[{msg.Sender}]: {msg.Content}");
}

// 取消订阅
private void OnDestroy()
{
    int chatCmdMerge = CmdKit.GetMergeCmd(2, 1);
    T2FGameSdk.Instance.Unsubscribe(chatCmdMerge);
}
```

### 5. 使用回调模式发送请求

```csharp
// 适合 UI 响应场景，避免 async/await 嵌套
public void OnLoginButtonClick()
{
    var request = new LoginRequest
    {
        Username = usernameInput.text,
        Password = passwordInput.text
    };

    int loginCmdMerge = CmdKit.GetMergeCmd(1, 1);

    // 发送请求并设置回调
    T2FGameSdk.Instance.Send<LoginRequest, LoginResponse>(
        loginCmdMerge,
        request,
        response =>
        {
            // 收到响应后的处理
            Debug.Log($"登录成功! Token: {response.Token}");
            EnterGameScene();
        }
    );
}
```

---

## 📚 API 文档

### T2FGameSdk（单例 SDK）

#### 初始化与连接

```csharp
// 初始化 SDK
void Initialize(GameClientOptions options = null)

// 连接到服务器（使用配置中的地址）
UniTask ConnectAsync()

// 连接到指定服务器（会覆盖配置）
UniTask ConnectAsync(string host, int port)

// 断开连接（可重连）
UniTask DisconnectAsync()

// 关闭连接（不再重连）
void Close()
```

#### 发送请求

```csharp
// 发送请求并等待响应（返回 ResponseMessage）
UniTask<ResponseMessage> RequestAsync(
    int cmdMerge,
    CancellationToken cancellationToken = default
)

// 发送 Protobuf 请求并等待响应
UniTask<ResponseMessage> RequestAsync<TRequest>(
    int cmdMerge,
    TRequest request,
    CancellationToken cancellationToken = default
) where TRequest : IMessage

// 发送请求并直接获取响应数据
UniTask<TResponse> RequestAsync<TResponse>(
    int cmdMerge,
    CancellationToken cancellationToken = default
) where TResponse : IMessage, new()

// 发送请求并获取指定类型的响应数据
UniTask<TResponse> RequestAsync<TRequest, TResponse>(
    int cmdMerge,
    TRequest request,
    CancellationToken cancellationToken = default
) where TRequest : IMessage where TResponse : IMessage, new()
```

#### 仅发送消息

```csharp
// 发送空消息
void Send(int cmdMerge)

// 发送 Protobuf 消息
void Send<TRequest>(int cmdMerge, TRequest request) where TRequest : IMessage

// 发送基础类型
void SendInt(int cmdMerge, int value)
void SendString(int cmdMerge, string value)
void SendLong(int cmdMerge, long value)
void SendBool(int cmdMerge, bool value)
```

#### 带回调的发送

```csharp
// 发送请求并在收到响应时执行回调
void Send<TRequest, TResponse>(
    int cmdMerge,
    TRequest request,
    Action<TResponse> callback
) where TRequest : IMessage where TResponse : IMessage, new()
```

#### 消息订阅

```csharp
// 订阅原始消息
void Subscribe(int cmdMerge, Action<ExternalMessage> callback)

// 订阅并自动解包为指定类型（推荐）
void Subscribe<TMessage>(int cmdMerge, Action<TMessage> callback)
    where TMessage : IMessage, new()

// 取消订阅（传 null 则取消该 cmdMerge 的所有订阅）
void Unsubscribe(int cmdMerge, Action<ExternalMessage> callback = null)

// 取消所有订阅
void UnsubscribeAll()
```

#### 属性与事件

```csharp
// 属性
bool IsConnected { get; }          // 是否已连接
bool IsInitialized { get; }        // 是否已初始化
ConnectionState State { get; }     // 当前连接状态
GameClient Client { get; }         // 底层客户端实例

// 事件
event Action<ConnectionState> OnStateChanged;    // 连接状态变化
event Action<ExternalMessage> OnMessageReceived; // 收到服务器推送
event Action<Exception> OnError;                 // 发生错误
```

---

### GameClientOptions（配置选项）

```csharp
public sealed class GameClientOptions
{
    // 基础配置
    public ChannelType ChannelType = ChannelType.Tcp;  // TCP/UDP/WebSocket
    public string Host = "localhost";                   // 服务器地址
    public int Port = 9090;                             // 服务器端口

    // 超时配置
    public int ConnectTimeoutMs = 10000;      // 连接超时（毫秒）
    public int RequestTimeoutMs = 30000;      // 请求超时（毫秒）

    // 心跳配置
    public int HeartbeatIntervalSec = 30;     // 心跳间隔（秒）
    public int HeartbeatTimeoutCount = 3;     // 心跳超时次数

    // 重连配置
    public bool AutoReconnect = true;         // 是否自动重连
    public int ReconnectIntervalSec = 3;      // 重连间隔（秒）
    public int MaxReconnectCount = 5;         // 最大重连次数（0=无限）

    // 缓冲区配置
    public int ReceiveBufferSize = 65536;     // 接收缓冲区大小
    public int SendBufferSize = 65536;        // 发送缓冲区大小

    // 其他配置
    public bool EnableLog = true;             // 是否启用日志
    public bool UseWorkerThread = true;       // 是否使用工作线程（WebGL 自动禁用）
}
```

---

### ResponseMessage（响应消息）

```csharp
public sealed class ResponseMessage
{
    // 属性
    public int CmdMerge { get; }           // 命令码
    public int MsgId { get; }              // 消息 ID
    public int ResponseStatus { get; }     // 响应状态码（0=成功）
    public bool Success { get; }           // 是否成功
    public bool HasError { get; }          // 是否有错误

    // 获取数据
    public T GetValue<T>() where T : IMessage, new();

    // 基础类型便捷方法
    public int GetInt();
    public long GetLong();
    public string GetString();
    public bool GetBool();
    public List<int> ListInt();
    public List<long> ListLong();
    public List<string> ListString();
    public List<bool> ListBool();
}
```

---

## 🎨 完整示例

### 登录 + 游戏逻辑示例

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using T2FGame.Client.Network;
using T2FGame.Client.Sdk;
using T2FGame.Protocol;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
    private CancellationTokenSource _cts;

    private async void Start()
    {
        _cts = new CancellationTokenSource();

        // 初始化并连接
        await InitializeAndConnect();

        // 登录
        bool loginSuccess = await Login("player123", "password");

        if (loginSuccess)
        {
            // 加载玩家数据
            await LoadPlayerData();

            // 开始游戏
            StartGame();
        }
    }

    private async UniTask InitializeAndConnect()
    {
        var options = new GameClientOptions
        {
            Host = "game.server.com",
            Port = 10100,
            ChannelType = ChannelType.Tcp,
            AutoReconnect = true,
            HeartbeatIntervalSec = 30,
            RequestTimeoutMs = 10000,
            EnableLog = true
        };

        T2FGameSdk.Instance.Initialize(options);

        // 订阅事件
        T2FGameSdk.Instance.OnStateChanged += OnConnectionStateChanged;
        T2FGameSdk.Instance.OnMessageReceived += OnServerPush;
        T2FGameSdk.Instance.OnError += OnNetworkError;

        // 订阅服务器推送消息
        SubscribeMessages();

        try
        {
            await T2FGameSdk.Instance.ConnectAsync();
            Debug.Log("✅ 连接服务器成功");
        }
        catch (TimeoutException)
        {
            Debug.LogError("❌ 连接超时");
            ShowErrorDialog("连接超时，请检查网络");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 连接失败：{ex.Message}");
            ShowErrorDialog("连接失败");
        }
    }

    private async UniTask<bool> Login(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            DeviceId = SystemInfo.deviceUniqueIdentifier
        };

        try
        {
            // 发送登录请求
            var response = await T2FGameSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
                cmdMerge: 1001,
                request: request,
                cancellationToken: _cts.Token
            );

            Debug.Log($"✅ 登录成功！UserId={response.UserId}, Token={response.Token}");

            // 保存 Token
            PlayerPrefs.SetString("AuthToken", response.Token);

            return true;
        }
        catch (TimeoutException)
        {
            Debug.LogError("❌ 登录超时");
            ShowErrorDialog("登录超时，请重试");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 登录失败：{ex.Message}");
            ShowErrorDialog("登录失败");
            return false;
        }
    }

    private async UniTask LoadPlayerData()
    {
        try
        {
            var playerData = await T2FGameSdk.Instance.RequestAsync<PlayerDataResponse>(
                cmdMerge: 1002,
                cancellationToken: _cts.Token
            );

            Debug.Log($"✅ 加载玩家数据成功：Level={playerData.Level}, Gold={playerData.Gold}");

            // 更新 UI
            UpdatePlayerUI(playerData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 加载玩家数据失败：{ex.Message}");
        }
    }

    private void StartGame()
    {
        Debug.Log("🎮 开始游戏");
        // 游戏逻辑...
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        Debug.Log($"📡 连接状态：{state}");

        switch (state)
        {
            case ConnectionState.Connected:
                HideReconnectDialog();
                break;

            case ConnectionState.Reconnecting:
                ShowReconnectDialog();
                break;

            case ConnectionState.Disconnected:
                ShowErrorDialog("连接已断开");
                break;
        }
    }

    private void OnServerPush(ExternalMessage message)
    {
        Debug.Log($"📨 收到服务器推送：CmdMerge={message.CmdMerge}");
    }

    private void SubscribeMessages()
    {
        // 使用 2.0 新增的订阅功能（推荐）
        T2FGameSdk.Instance.Subscribe<ChatMessage>(3001, OnChatMessage);
        T2FGameSdk.Instance.Subscribe<SystemNotification>(3002, OnSystemNotification);
        T2FGameSdk.Instance.Subscribe<GoldChangeNotification>(3003, OnGoldChanged);
    }

    private void OnChatMessage(ChatMessage chatMsg)
    {
        Debug.Log($"💬 [{chatMsg.Sender}]: {chatMsg.Content}");
        // 显示聊天消息...
    }

    private void OnSystemNotification(SystemNotification notification)
    {
        ShowNotification(notification.Message);
    }

    private void OnGoldChanged(GoldChangeNotification goldChange)
    {
        Debug.Log($"💰 金币变化：{goldChange.Delta} (总计: {goldChange.TotalGold})");
        UpdateGoldUI(goldChange.TotalGold);
    }

    private void OnNetworkError(Exception ex)
    {
        Debug.LogError($"❌ 网络错误：{ex.Message}");
    }

    private void OnDestroy()
    {
        // 清理
        _cts?.Cancel();
        _cts?.Dispose();

        // 取消事件订阅
        T2FGameSdk.Instance.OnStateChanged -= OnConnectionStateChanged;
        T2FGameSdk.Instance.OnMessageReceived -= OnServerPush;
        T2FGameSdk.Instance.OnError -= OnNetworkError;

        // 取消消息订阅
        T2FGameSdk.Instance.UnsubscribeAll();

        T2FGameSdk.Instance.Close();
    }

    // UI 相关方法（示意）
    private void UpdatePlayerUI(PlayerDataResponse data) { }
    private void UpdateGoldUI(long gold) { }
    private void ShowErrorDialog(string message) { }
    private void ShowNotification(string message) { }
    private void ShowReconnectDialog() { }
    private void HideReconnectDialog() { }
}
```

---

## 🏗️ 架构设计

### 模块化架构

T2FGameSdk 采用**职责分离**的三层管理器架构：

```
┌────────────────────────────────────────────────────┐
│              T2FGameSdk (主入口)                    │
│  - 单例模式                                         │
│  - 初始化和生命周期管理                              │
│  - 事件转发和协调                                   │
└────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ConnectionMgr │ │ MessageRouter│ │ RequestMgr   │
│              │ │              │ │              │
│- 连接管理    │ │- 消息路由    │ │- 请求管理    │
│- 状态监控    │ │- 订阅/分发   │ │- 回调处理    │
│- 重连逻辑    │ │- 高效分发    │ │- 超时处理    │
└──────────────┘ └──────────────┘ └──────────────┘
```

**核心优势**：
- ✅ **职责分离**：每个管理器专注单一职责
- ✅ **高性能**：MessageRouter 使用 multicast delegate，零分配
- ✅ **可测试**：每个管理器可独立测试
- ✅ **可扩展**：易于添加新管理器

详细架构说明：[ARCHITECTURE.md](Runtime/Sdk/ARCHITECTURE.md)

### 分层架构

```
┌─────────────────────────────────────────────┐
│           业务逻辑层（Game Logic）           │
│         (登录、战斗、聊天等业务代码)          │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│         SDK 层（T2FGameSdk）                 │
│  ConnectionMgr + MessageRouter + RequestMgr │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│       客户端层（GameClient）                 │
│  (连接管理、消息路由、心跳、重连)              │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│       传输层（IProtocolChannel）              │
│    (TCP/UDP/WebSocket 协议实现)              │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│         协议层（PacketCodec）                 │
│      (ioGame 协议编解码、粘包处理)            │
└─────────────────────────────────────────────┘
```

### 核心组件

| 组件 | 职责 |
|------|------|
| **T2FGameSdk** | SDK 主入口，单例管理，提供高层 API |
| **ConnectionManager** | 连接管理、状态监控、自动重连 |
| **MessageRouter** | 消息路由、订阅管理、高效分发 |
| **RequestManager** | 请求发送、回调处理、超时管理 |
| **GameClient** | 核心客户端，管理连接、消息、心跳、重连 |
| **IProtocolChannel** | 传输层抽象接口，支持多种协议 |
| **PacketCodec** | ioGame 协议编解码器 |
| **PacketBuffer** | TCP 粘包处理缓冲区 |
| **RequestCommand** | 请求命令封装 |
| **ResponseMessage** | 响应消息封装 |
| **MsgIdManager** | 消息 ID 生成器（线程安全） |

### 线程模型

```
主线程（Unity Main Thread）
  ├── SDK 初始化
  ├── 事件回调（OnStateChanged, OnMessageReceived...）
  └── UniTask 异步任务

工作线程（仅 TCP/UDP，WebGL 禁用）
  ├── Socket 接收
  └── Socket 发送
```

---

## 🔧 平台适配

### 支持的平台

| 平台 | TCP | UDP | WebSocket | 推荐协议 |
|------|-----|-----|-----------|----------|
| **Windows** | ✅ | ✅ | ✅ | TCP |
| **macOS** | ✅ | ✅ | ✅ | TCP |
| **Linux** | ✅ | ✅ | ✅ | TCP |
| **Android** | ✅ | ✅ | ✅ | TCP |
| **iOS** | ✅ | ✅ | ✅ | TCP |
| **WebGL** | ❌ | ❌ | ✅ | **WebSocket** |
| **微信小游戏** | ❌ | ❌ | ✅ | **WebSocket** |

### WebGL 特殊配置

```csharp
#if UNITY_WEBGL
var options = new GameClientOptions
{
    ChannelType = ChannelType.WebSocket,  // 必须使用 WebSocket
    Host = "wss://game.server.com",       // WebSocket URL
    Port = 443,
    UseWorkerThread = false               // 自动禁用（WebGL 不支持多线程）
};
#endif
```

---

## ❓ 常见问题

### Q1: 如何处理断线重连？

**A**: SDK 默认开启自动重连，业务层只需监听状态变化：

```csharp
T2FGameSdk.Instance.OnStateChanged += (state) =>
{
    if (state == ConnectionState.Connected)
    {
        // 重连成功，可能需要重新登录或同步数据
        ReLoginOrSyncData();
    }
};
```

### Q2: 如何取消正在进行的请求？

**A**: 使用 `CancellationToken`：

```csharp
var cts = new CancellationTokenSource();

var task = T2FGameSdk.Instance.RequestAsync<MyResponse>(
    cmdMerge: 1001,
    cancellationToken: cts.Token
);

// 5 秒后取消
await UniTask.Delay(5000);
cts.Cancel();
```

### Q3: 如何区分不同的错误类型？

**A**: 通过捕获不同的异常类型：

```csharp
try
{
    var response = await T2FGameSdk.Instance.RequestAsync(...);
}
catch (TimeoutException)
{
    // 超时
}
catch (OperationCanceledException)
{
    // 取消
}
catch (InvalidOperationException ex)
{
    // 未连接或其他状态错误
}
```

### Q4: 如何处理服务器返回的业务错误码？

**A**: 检查 `ResponseMessage.ResponseStatus`：

```csharp
var response = await T2FGameSdk.Instance.RequestAsync<MyResponse>(...);

if (response.ResponseStatus != 0)
{
    // 服务器返回错误
    switch (response.ResponseStatus)
    {
        case 1001:
            Debug.LogError("密码错误");
            break;
        case 1002:
            Debug.LogError("账号不存在");
            break;
    }
    return;
}

// 成功处理
var data = response.GetValue<MyResponse>();
```

### Q5: 为什么 WebGL 平台连接失败？

**A**: WebGL 平台只支持 WebSocket，请确保：

1. 使用 `ChannelType.WebSocket`
2. Host 使用完整 URL（`ws://` 或 `wss://`）
3. 服务器支持 WebSocket 协议

---

## 🔬 性能优化建议

### 1. 对象池

SDK 已内置对象池优化，业务层无需关心。

### 2. 减少频繁请求

避免在 `Update()` 中频繁发送请求：

```csharp
// ❌ 不推荐
void Update()
{
    SendHeartbeat(); // 每帧发送
}

// ✅ 推荐：使用定时器
private async UniTaskVoid HeartbeatLoop()
{
    while (true)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(30));
        SendHeartbeat();
    }
}
```

### 3. 批量发送

如果有多条消息，考虑在服务器端支持批量接口。

### 4. 日志级别

生产环境关闭日志：

```csharp
var options = new GameClientOptions
{
    EnableLog = false  // 关闭日志，提升性能
};
```

---


## 📄 许可证

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📮 联系方式

- **作者**：unittt
- **GitHub**：[https://github.com/unittt](https://github.com/unittt)

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给个 Star！⭐**

Made with ❤️ by unittt

</div>
