# Pisces Client SDK

<div align="center">

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**高性能、模块化、跨平台的 Unity 游戏客户端网络 SDK**

[特性](#-核心特性) • [安装](#-安装) • [快速开始](#-快速开始) • [文档](#-api-文档) • [示例](#-完整示例)

</div>

---

## 📖 项目简介

Pisces Client SDK 是一个专为 Unity 游戏开发设计的**独立、轻量、高性能**的网络通信框架。它基于 **Protobuf协议**，提供完整的客户端网络功能，包括连接管理、消息收发、心跳保活、自动重连等。

### 设计理念

- **🎯 独立性**：零业务依赖，不依赖任何游戏框架，可在任意 Unity 项目中使用
- **🏗️ 模块化**：采用三层管理器架构（连接、路由、请求分离），职责清晰，易于维护
- **⚡ 高性能**：基于 UniTask 的异步编程，multicast delegate 高效订阅机制
- **🌐 跨平台**：支持 TCP、UDP、WebSocket，适配桌面、移动、WebGL 等所有平台
- **🔧 易用性**：简洁的 API 设计，符合 C#/.NET 最佳实践
- **🛡️ 可靠性**：完善的错误处理、自动重连、心跳保活机制

---

## ✨ 核心特性

### 网络通信
- ✅ **多协议支持**：TCP、UDP、WebSocket（自动适配平台）
- ✅ **Protobuf 序列化**：基于 Protobuf协议的高效序列化
- ✅ **请求-响应模型**：支持 async/await 异步请求，自动匹配响应
- ✅ **回调模式**：支持 Send<TRequest, TResponse>(callback) 回调式请求
- ✅ **服务器推送**：支持 cmdMerge 消息订阅和自动分发
- ✅ **对象池支持**：支持直接发送 RequestCommand 对象，配合对象池减少 GC

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
https://github.com/PiscesGameDev/Pisces.Client.Unity.git
```

### 方式 2：本地安装

1. 下载本仓库到本地
2. 打开 Unity Package Manager
3. 点击 `+` → `Add package from disk...`
4. 选择 `package.json` 文件

### 方式 3：直接复制

将整个 `Pisces.Client.Unity` 文件夹复制到项目的 `Assets` 目录下。

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


---

## 🚀 快速开始

### 1. 基础用法

```csharp
using Cysharp.Threading.Tasks;
using Pisces.Client.Network;
using Pisces.Client.Sdk;
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
        PiscesSdk.Instance.Initialize(options);

        // 3. 订阅事件
        PiscesSdk.Instance.OnStateChanged += OnConnectionStateChanged;

        // 4. 连接服务器
        try
        {
            await PiscesSdk.Instance.ConnectAsync();
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
using Pisces.Protocol; // 你的 Protobuf 消息定义

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
            var response = await PiscesSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
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
    PiscesSdk.Instance.Send(cmdMerge: 1);
}

public void SendChatMessage(string message)
{
    var chatMsg = new ChatMessage { Content = message };
    PiscesSdk.Instance.Send(cmdMerge: 2001, chatMsg);
}
```

### 4. 订阅服务器推送消息

```csharp
// 定义 handler 引用（用于后续取消订阅）
private Action<ChatMessage> _chatHandler;

private void Start()
{
    int chatCmdMerge = CmdKit.GetMergeCmd(2, 1);

    // 方式 1: 订阅并自动解包为指定类型（推荐）
    _chatHandler = OnChatMessage;
    PiscesSdk.Instance.Subscribe(chatCmdMerge, _chatHandler);

    // 方式 2: 订阅原始消息
    PiscesSdk.Instance.Subscribe(chatCmdMerge, message =>
    {
        var chatMsg = ProtoSerializer.Deserialize<ChatMessage>(message.Data);
        Debug.Log($"收到聊天: {chatMsg.Content}");
    });
}

private void OnChatMessage(ChatMessage msg)
{
    Debug.Log($"[{msg.Sender}]: {msg.Content}");
}

private void OnDestroy()
{
    int chatCmdMerge = CmdKit.GetMergeCmd(2, 1);

    // 取消特定订阅（必须使用订阅时的 handler 引用）
    PiscesSdk.Instance.Unsubscribe(chatCmdMerge, _chatHandler);

    // 或者清除该 cmdMerge 的所有订阅
    PiscesSdk.Instance.UnsubscribeAll(chatCmdMerge);
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

    // 有请求体，泛型响应回调
    PiscesSdk.Instance.Send<LoginRequest, LoginResponse>(
        loginCmdMerge,
        request,
        response =>
        {
            Debug.Log($"登录成功! Token: {response.Token}");
            EnterGameScene();
        }
    );
}

// 无请求体，泛型响应回调
public void OnRefreshDataClick()
{
    int refreshCmdMerge = CmdKit.GetMergeCmd(1, 2);

    PiscesSdk.Instance.Send<PlayerDataResponse>(
        refreshCmdMerge,
        response =>
        {
            Debug.Log($"刷新成功! Level: {response.Level}");
            UpdateUI(response);
        }
    );
}

// 无请求体，原始响应回调
public void OnPingClick()
{
    int pingCmdMerge = CmdKit.GetMergeCmd(1, 3);

    PiscesSdk.Instance.Send(
        pingCmdMerge,
        response =>
        {
            if (response.Success)
            {
                Debug.Log("Ping 成功!");
            }
            else
            {
                Debug.LogError($"Ping 失败! 错误码: {response.ResponseStatus}");
            }
        }
    );
}

// 有请求体，原始响应回调（适用于需要检查状态码的场景）
public void OnLogoutClick()
{
    var request = new LogoutRequest
    {
        UserId = currentUserId
    };

    int logoutCmdMerge = CmdKit.GetMergeCmd(1, 4);

    PiscesSdk.Instance.Send(
        logoutCmdMerge,
        request,
        response =>
        {
            if (response.Success)
            {
                Debug.Log("登出成功!");
                ReturnToLoginScene();
            }
            else
            {
                Debug.LogWarning($"登出失败，状态码: {response.ResponseStatus}");
            }
        }
    );
}
```

### 6. 使用 RequestCommand（对象池自动管理）

```csharp
using Pisces.Client.Sdk;

// 框架会自动管理对象池，开发者只需 Of 创建，无需手动回收
public class AdvancedRequestExample : MonoBehaviour
{
    // 方式 1: async/await
    private async UniTask LoginWithCommand()
    {
        var loginRequest = new LoginRequest
        {
            Username = "player123",
            Password = "password"
        };

        // 创建 RequestCommand（内部使用对象池）
        var command = RequestCommand.Of(
            cmdMerge: CmdKit.GetMergeCmd(1, 1),
            message: loginRequest
        );

        // 发送后框架会自动回收，无需手动 Despawn(切勿在发送后继续使用 command)
        var response = await PiscesSdk.Instance.RequestAsync(command);
        if (response.Success)
        {
            var loginResp = response.GetValue<LoginResponse>();
            Debug.Log($"登录成功: {loginResp.UserId}");
        }
    }

    // 方式 2: 回调模式
    private void SendWithCallback()
    {
        var request = new GetPlayerDataRequest { UserId = "123" };
        var command = RequestCommand.Of(CmdKit.GetMergeCmd(1, 2), request);

        // 框架会在回调执行后自动回收 command
        PiscesSdk.Instance.Send(command, response =>
        {
            if (response.Success)
            {
                var data = response.GetValue<PlayerDataResponse>();
                Debug.Log($"玩家数据: Level={data.Level}");
            }
        });
    }

    // 方式 3: Fire-and-forget
    private void SendFireAndForget()
    {
        // 心跳消息（框架会自动回收）
        var heartbeatCmd = RequestCommand.Heartbeat();
        PiscesSdk.Instance.Send(heartbeatCmd);
    }

    // 批量并发请求
    private async UniTask BatchRequest()
    {
        // 创建多个命令
        var tasks = new List<UniTask<ResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            var cmd = RequestCommand.Of(
                CmdKit.GetMergeCmd(3, 1),
                new GetItemRequest { ItemId = i }
            );
            // 每个请求都会自动回收
            tasks.Add(PiscesSdk.Instance.RequestAsync(cmd));
        }

        // 等待所有请求完成
        var responses = await UniTask.WhenAll(tasks);

        // 处理响应
        foreach (var response in responses)
        {
            if (response.Success)
            {
                var item = response.GetValue<ItemResponse>();
                Debug.Log($"获得物品: {item.ItemName}");
            }
        }
    }

    // 使用基础类型便捷方法（推荐，更简洁）
    private async UniTask SendWithBuiltInTypes()
    {
        // 直接发送 int（框架内部使用 RequestCommand + 对象池）
        await PiscesSdk.Instance.RequestAsync(
            CmdKit.GetMergeCmd(1, 1),
            100  // 自动转换为 IntValue
        );

        // 直接发送 string
        await PiscesSdk.Instance.RequestAsync(
            CmdKit.GetMergeCmd(1, 2),
            "hello"  // 自动转换为 StringValue
        );

        // 以上方法内部都自动使用对象池
    }
}
```

**重要说明**：
- ✅ **框架自动回收**：`RequestCommand` 在发送后会被框架自动归还到对象池
- ✅ **开发者无需管理**：只需调用 `RequestCommand.Of()` 创建，无需手动 `Despawn`
- ⚠️ **禁止手动回收**：不要手动调用 `ReferencePool<RequestCommand>.Despawn()`，会导致双重回收错误
- ✅ **推荐使用内置方法**：对于简单场景，优先使用 `RequestAsync<TRequest>(...)` 等便捷方法
- ✅ **RequestCommand 适用场景**：复杂请求构建、批量操作、需要精细控制的场景

---

## 📚 API 文档

### PiscesSdk（单例 SDK）

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

// 直接发送 RequestCommand 对象（支持对象池）
UniTask<ResponseMessage> RequestAsync(
    RequestCommand command,
    CancellationToken cancellationToken = default
)
```

#### 仅发送消息

```csharp
// 发送空消息
void Send(int cmdMerge)

// 发送 Protobuf 消息
void Send<TRequest>(int cmdMerge, TRequest request) where TRequest : IMessage

// 直接发送 RequestCommand 对象（fire-and-forget，支持对象池）
void Send(RequestCommand command)

// 发送基础类型
void SendInt(int cmdMerge, int value)
void SendString(int cmdMerge, string value)
void SendLong(int cmdMerge, long value)
void SendBool(int cmdMerge, bool value)
```

#### 带回调的发送

```csharp
// 发送请求并在收到响应时执行回调（无请求体，原始响应）
void Send(int cmdMerge, Action<ResponseMessage> callback)

// 发送请求并在收到响应时执行回调（无请求体，泛型响应）
void Send<TResponse>(int cmdMerge, Action<TResponse> callback)
    where TResponse : IMessage, new()

// 发送请求并在收到响应时执行回调（有请求体，原始响应）
void Send<TRequest>(
    int cmdMerge,
    TRequest request,
    Action<ResponseMessage> callback
) where TRequest : IMessage

// 发送请求并在收到响应时执行回调（有请求体，泛型响应）
void Send<TRequest, TResponse>(
    int cmdMerge,
    TRequest request,
    Action<TResponse> callback
) where TRequest : IMessage where TResponse : IMessage, new()

// 直接发送 RequestCommand 对象并在收到响应时执行回调（支持对象池）
void Send(RequestCommand command, Action<ResponseMessage> callback)
```

#### 消息订阅

```csharp
// 订阅原始消息
void Subscribe(int cmdMerge, Action<ExternalMessage> callback)

// 订阅并自动解包为指定类型（推荐）
void Subscribe<TMessage>(int cmdMerge, Action<TMessage> callback)
    where TMessage : IMessage, new()

// 取消订阅（必须传入订阅时的 handler 引用）
void Unsubscribe(int cmdMerge, Action<ExternalMessage> callback)

// 取消订阅（泛型版本，必须传入订阅时的 handler 引用）
void Unsubscribe<TMessage>(int cmdMerge, Action<TMessage> callback)
    where TMessage : IMessage, new()

// 清除指定 cmdMerge 的所有订阅
void UnsubscribeAll(int cmdMerge)

// 清除所有订阅
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
    public string ErrorMessage { get; }    // 错误消息（来自服务器）

    // 获取数据（支持缓存机制，同一类型多次调用不会重复反序列化）
    public T GetValue<T>() where T : IMessage, new();

    // 基础类型便捷方法（零 GC + 空安全）
    public int GetInt();
    public long GetLong();
    public string GetString();
    public bool GetBool();

    // 列表方法（利用缓存，后续调用避免重复反序列化）
    public IReadOnlyList<int> ListInt();
    public IReadOnlyList<long> ListLong();
    public IReadOnlyList<string> ListString();
    public IReadOnlyList<bool> ListBool();

    // Unity Vector 类型支持
    public UnityEngine.Vector2 GetVector2();
    public UnityEngine.Vector2Int GetVector2Int();
    public UnityEngine.Vector3 GetVector3();
    public UnityEngine.Vector3Int GetVector3Int();

    // Vector 列表方法（利用缓存，后续调用避免重复反序列化）
    public IReadOnlyList<Vector2> ListVector2();
    public IReadOnlyList<Vector2Int> ListVector2Int();
    public IReadOnlyList<Vector3> ListVector3();
    public IReadOnlyList<Vector3Int> ListVector3Int();

    // 字典访问器（复用传入的字典容器）
    public void GetDictionary<T>(Dictionary<int, T> result) where T : IMessage, new();
    public void GetDictionary<T>(Dictionary<long, T> result) where T : IMessage, new();
    public void GetDictionary<T>(Dictionary<string, T> result) where T : IMessage, new();
}
```

**性能优化说明**：
- ✅ `GetValue<T>()` 内置了单值缓存机制，同一响应对象重复调用相同类型不会重复反序列化
- ✅ 所有 List 方法返回 `IReadOnlyList<T>`，直接引用内部数据，**后续调用避免重复反序列化**
- ✅ 所有 Get 方法增加**空值安全**，不会抛出 NullReferenceException
- ✅ 对象池回收时自动清理缓存，无需手动管理
- ✅ 性能提升：重复调用利用缓存避免重复反序列化，List 方法避免 ToList() 的额外拷贝

**使用示例**：
```csharp
UserAction.OfLogin(request, result => {
    // 错误处理
    if (result.HasError) {
        Debug.LogError($"登录失败: {result.ErrorMessage}");  // 显示服务器错误消息
        return;
    }

    // 获取数据（第1次调用：反序列化 + 缓存）
    var data = result.GetValue<LoginResponse>();

    // 重复调用直接从缓存返回，无额外开销
    var data2 = result.GetValue<LoginResponse>();

    // 列表数据（利用缓存，后续调用避免重复反序列化）
    var scores = result.ListInt();        // IReadOnlyList<int>
    foreach (var score in scores) {     
        ProcessScore(score);
    }

    // Vector 类型
    var position = result.GetVector3();   // 获取玩家位置
    transform.position = position;        // 直接使用

    // Vector 列表（利用缓存）
    var waypoints = result.ListVector3(); // IReadOnlyList<Vector3>
    for (int i = 0; i < waypoints.Count; i++) {
        pathNodes[i].position = waypoints[i];
    }

    // 字典数据（复用容器）
    var items = new Dictionary<int, ItemData>();
    result.GetDictionary(items);  // 填充到传入的字典
    foreach (var kvp in items) {
        Debug.Log($"Item {kvp.Key}: {kvp.Value.Name}");
    }
});
```

---

### Protobuf Wrapper 类型隐式转换 ✨

SDK 为 Protobuf 的基础类型包装器提供了隐式转换支持，大幅简化代码编写。

**支持的类型**：
- `IntValue` ↔ `int`
- `LongValue` ↔ `long`
- `StringValue` ↔ `string`
- `BoolValue` ↔ `bool`
- `IntValueList` ↔ `List<int>` / `int[]`
- `LongValueList` ↔ `List<long>` / `long[]`
- `StringValueList` ↔ `List<string>` / `string[]`
- `BoolValueList` ↔ `List<bool>` / `bool[]`
- `Vector2` ↔ `UnityEngine.Vector2`
- `Vector2Int` ↔ `UnityEngine.Vector2Int`
- `Vector3` ↔ `UnityEngine.Vector3`
- `Vector3Int` ↔ `UnityEngine.Vector3Int`
- `Vector2List` ↔ `List<UnityEngine.Vector2>` / `Vector2[]`
- `Vector2IntList` ↔ `List<UnityEngine.Vector2Int>` / `Vector2Int[]`
- `Vector3List` ↔ `List<UnityEngine.Vector3>` / `Vector3[]`
- `Vector3IntList` ↔ `List<UnityEngine.Vector3Int>` / `Vector3Int[]`

**使用示例**：

```csharp
// 场景 1：Protobuf 消息字段赋值
public class UpdateScoreRequest : IMessage<UpdateScoreRequest>
{
    public IntValue Score { get; set; }
    public StringValue Reason { get; set; }
    public Vector3 Position { get; set; }
}

// 隐式转换，简洁优雅
var request = new UpdateScoreRequest
{
    Score = 100,                              // int → IntValue
    Reason = "victory",                       // string → StringValue
    Position = new Vector3(10f, 20f, 30f)     // Vector3 → Proto.Vector3
};

// 场景 2：方法参数传递
void ProcessScore(IntValue score) { }
void MovePlayer(Vector3 position) { }

ProcessScore(100);                  // int 隐式转换为 IntValue
MovePlayer(transform.position);     // UnityEngine.Vector3 隐式转换

// 场景 3：从 Wrapper 提取值
IntValue scoreWrapper = response.GetValue<IntValue>();
Vector3 posWrapper = response.GetValue<Vector3>();

int score = scoreWrapper;                      // IntValue → int
UnityEngine.Vector3 pos = posWrapper;          // Proto.Vector3 → UnityEngine.Vector3

// 场景 4：Unity 特有的向量操作
var moveRequest = new MoveRequest
{
    Position = transform.position,             // UnityEngine.Vector3 → Proto.Vector3
    Direction = transform.forward              // 无缝集成 Unity API
};

// 场景 5：列表类型转换
IntValueList scores = new int[] { 1, 2, 3 };               // int[] → IntValueList
Vector3List positions = new Vector3[] {                    // Vector3[] → Vector3List
    new Vector3(0, 0, 0),
    new Vector3(1, 1, 1)
};

List<int> scoreList = scores;                              // IntValueList → List<int>
List<UnityEngine.Vector3> posList = positions;             // Vector3List → List<UnityEngine.Vector3>
```

**性能说明**：
- 隐式转换在编译时解析，运行时性能与手动构造完全相同
- 无额外内存开销
- 推荐在所有场景中使用

### 字典类型转换（使用 From 方法）

由于 C# 隐式转换操作符不支持泛型参数，字典类型使用静态 `From<T>()` 方法进行转换。

**支持的类型**：
- `IntKeyMap.From<T>(Dictionary<int, T>)` ← int 键字典
- `LongKeyMap.From<T>(Dictionary<long, T>)` ← long 键字典
- `StringKeyMap.From<T>(Dictionary<string, T>)` ← string 键字典

**使用示例**：

```csharp
// 创建字典数据
var playerItems = new Dictionary<int, ItemData>
{
    { 1001, new ItemData { Name = "剑", Count = 1 } },
    { 1002, new ItemData { Name = "盾", Count = 2 } }
};

// 转换为 Protobuf Map 类型
var map = IntKeyMap.From(playerItems);

// 直接用于请求
PiscesSdk.Instance.Send(cmdMerge, IntKeyMap.From(playerItems));

// 反向转换：从 Map 提取到字典（复用容器）
var result = new Dictionary<int, ItemData>();
map.ToDictionary(result);
```

**为什么不用隐式转换**：
```csharp
// ❌ C# 不允许隐式转换操作符使用泛型参数
public static implicit operator IntKeyMap<T>(Dictionary<int, T> dict) 
    where T : IMessage<T>  // 编译错误！
```

---

## 🎨 完整示例

### 登录 + 游戏逻辑示例

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pisces.Client.Network;
using Pisces.Client.Sdk;
using Pisces.Protocol;
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

        PiscesSdk.Instance.Initialize(options);

        // 订阅事件
        PiscesSdk.Instance.OnStateChanged += OnConnectionStateChanged;
        PiscesSdk.Instance.OnMessageReceived += OnServerPush;
        PiscesSdk.Instance.OnError += OnNetworkError;

        // 订阅服务器推送消息
        SubscribeMessages();

        try
        {
            await PiscesSdk.Instance.ConnectAsync();
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
            var response = await PiscesSdk.Instance.RequestAsync<LoginRequest, LoginResponse>(
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
            var playerData = await PiscesSdk.Instance.RequestAsync<PlayerDataResponse>(
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

    // 保存 handler 引用，用于取消订阅
    private Action<ChatMessage> _chatHandler;
    private Action<SystemNotification> _systemHandler;
    private Action<GoldChangeNotification> _goldHandler;

    private void SubscribeMessages()
    {
        _chatHandler = OnChatMessage;
        _systemHandler = OnSystemNotification;
        _goldHandler = OnGoldChanged;

        PiscesSdk.Instance.Subscribe(3001, _chatHandler);
        PiscesSdk.Instance.Subscribe(3002, _systemHandler);
        PiscesSdk.Instance.Subscribe(3003, _goldHandler);
    }

    private void UnsubscribeMessages()
    {
        PiscesSdk.Instance.Unsubscribe(3001, _chatHandler);
        PiscesSdk.Instance.Unsubscribe(3002, _systemHandler);
        PiscesSdk.Instance.Unsubscribe(3003, _goldHandler);
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
        PiscesSdk.Instance.OnStateChanged -= OnConnectionStateChanged;
        PiscesSdk.Instance.OnMessageReceived -= OnServerPush;
        PiscesSdk.Instance.OnError -= OnNetworkError;

        // 取消消息订阅（使用保存的 handler 引用）
        UnsubscribeMessages();

        PiscesSdk.Instance.Close();
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

PiscesSdk 采用**职责分离**的三层管理器架构：

```
┌────────────────────────────────────────────────────┐
│              PiscesSdk (主入口)                   │
│  - 单例模式                                        │
│  - 初始化和生命周期管理                            │
│  - 事件转发和协调                                  │
└────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ConnectionMgr │ │ MessageRouter│ │ RequestMgr   │
│              │ │              │ │              │
│- 连接管理    │ │- 消息路由    │ │- 请求管理     │
│- 状态监控    │ │- 订阅/分发   │ │- 回调处理     │
│- 重连逻辑    │ │- 高效分发    │ │- 超时处理     │
└──────────────┘ └──────────────┘ └──────────────┘
```

### 分层架构

```
┌─────────────────────────────────────────────┐
│           业务逻辑层（Game Logic）          │
│         (登录、战斗、聊天等业务代码)        │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│         SDK 层（PiscesSdk）                │
│  ConnectionMgr + MessageRouter + RequestMgr │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│        客户端层（GameClient）               │
│  (连接管理、消息路由、心跳、重连)            │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│       传输层（IProtocolChannel）            │
│      (TCP/UDP/WebSocket 协议实现)           │
└─────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────┐
│         协议层（PacketCodec）               │
│         (协议编解码、粘包处理)               
└─────────────────────────────────────────────┘
```

### 核心组件

| 组件 | 职责 |
|------|------|
| **PiscesSdk** | SDK 主入口，单例管理，提供高层 API |
| **ConnectionManager** | 连接管理、状态监控、自动重连 |
| **MessageRouter** | 消息路由、订阅管理、高效分发 |
| **RequestManager** | 请求发送、回调处理、超时管理 |
| **GameClient** | 核心客户端，管理连接、消息、心跳、重连 |
| **IProtocolChannel** | 传输层抽象接口，支持多种协议 |
| **PacketCodec** | 协议编解码器 |
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
PiscesSdk.Instance.OnStateChanged += (state) =>
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

var task = PiscesSdk.Instance.RequestAsync<MyResponse>(
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
    var response = await PiscesSdk.Instance.RequestAsync(...);
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
var response = await PiscesSdk.Instance.RequestAsync<MyResponse>(...);

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
