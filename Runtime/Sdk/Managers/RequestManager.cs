using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Pisces.Client.Utils;

namespace Pisces.Client.Sdk
{
    /// <summary>
    /// 请求管理器
    /// 负责管理网络请求的发送、响应处理、回调执行等
    /// </summary>
    internal sealed class RequestManager
    {
        private readonly ConnectionManager _connectionManager;

        /// <summary>
        /// 请求错误事件
        /// </summary>
        public event Action<Exception> OnError;

        public RequestManager(ConnectionManager connectionManager)
        {
            _connectionManager =
                connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        #region 异步请求 (Async)

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(
            int cmdMerge,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConnected();
            var command = RequestCommand.Of(cmdMerge);
            return await _connectionManager.Client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync<TRequest>(int cmdMerge, TRequest request, CancellationToken cancellationToken = default) where TRequest : IMessage
        {
            EnsureConnected();
            var command = RequestCommand.Of(cmdMerge, request);
            return await _connectionManager.Client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 直接发送 RequestCommand 并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(
            RequestCommand command,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConnected();
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return await _connectionManager.Client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        public async UniTask<TResponse> RequestAsync<TResponse>(
            int cmdMerge,
            CancellationToken cancellationToken = default
        )
            where TResponse : IMessage, new()
        {
            var response = await RequestAsync(cmdMerge, cancellationToken);

            if (response.HasError)
            {
                throw new Exception($"Request failed with status: {response.ResponseStatus}");
            }

            return response.GetValue<TResponse>();
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        public async UniTask<TResponse> RequestAsync<TRequest, TResponse>(
            int cmdMerge,
            TRequest request,
            CancellationToken cancellationToken = default
        )
            where TRequest : IMessage
            where TResponse : IMessage, new()
        {
            var response = await RequestAsync(cmdMerge, request, cancellationToken);

            if (response.HasError)
            {
                throw new Exception($"Request failed with status: {response.ResponseStatus}");
            }

            return response.GetValue<TResponse>();
        }

        #endregion

        #region 发送不等待响应 (Fire and Forget)

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send(int cmdMerge)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send<TRequest>(int cmdMerge, TRequest request)
            where TRequest : IMessage
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge, request);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 直接发送 RequestCommand（仅发送，不等待响应）
        /// </summary>
        public void Send(RequestCommand command)
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (command == null)
            {
                GameLogger.LogWarning("[RequestManager] RequestCommand 不能为 null");
                return;
            }

            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送整数请求
        /// </summary>
        public void SendInt(int cmdMerge, int value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送字符串请求
        /// </summary>
        public void SendString(int cmdMerge, string value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送长整数请求
        /// </summary>
        public void SendLong(int cmdMerge, long value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送布尔值请求
        /// </summary>
        public void SendBool(int cmdMerge, bool value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.Of(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        #endregion

        #region 带回调的发送 (Callback)

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，原始响应）
        /// </summary>
        public void Send(int cmdMerge, Action<ResponseMessage> callback)
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (callback == null)
            {
                Send(cmdMerge);
                return;
            }

            SendWithCallbackAsync(cmdMerge, callback).Forget();
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，泛型响应）
        /// </summary>
        public void Send<TResponse>(int cmdMerge, Action<TResponse> callback)
            where TResponse : IMessage, new()
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (callback == null)
            {
                Send(cmdMerge);
                return;
            }

            SendWithCallbackAsync<TResponse>(cmdMerge, callback).Forget();
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，泛型响应）
        /// </summary>
        public void Send<TRequest, TResponse>(
            int cmdMerge,
            TRequest request,
            Action<TResponse> callback
        )
            where TRequest : IMessage
            where TResponse : IMessage, new()
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (callback == null)
            {
                Send(cmdMerge, request);
                return;
            }

            SendWithCallbackAsync(cmdMerge, request, callback).Forget();
        }

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，原始响应）
        /// </summary>
        public void Send<TRequest>(
            int cmdMerge,
            TRequest request,
            Action<ResponseMessage> callback
        )
            where TRequest : IMessage
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (callback == null)
            {
                Send(cmdMerge, request);
                return;
            }

            SendWithCallbackAsync(cmdMerge, request, callback).Forget();
        }

        /// <summary>
        /// 直接发送 RequestCommand 并在收到响应时执行回调
        /// </summary>
        public void Send(RequestCommand command, Action<ResponseMessage> callback)
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                return;
            }

            if (command == null)
            {
                GameLogger.LogWarning("[RequestManager] RequestCommand 不能为 null");
                return;
            }

            if (callback == null)
            {
                Send(command);
                return;
            }

            SendWithCallbackAsync(command, callback).Forget();
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法（无请求体，原始响应）
        /// </summary>
        private async UniTaskVoid SendWithCallbackAsync(
            int cmdMerge,
            Action<ResponseMessage> callback
        )
        {
            try
            {
                var response = await RequestAsync(cmdMerge);
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 带回调的发送失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法（无请求体，泛型响应）
        /// </summary>
        private async UniTaskVoid SendWithCallbackAsync<TResponse>(
            int cmdMerge,
            Action<TResponse> callback
        )
            where TResponse : IMessage, new()
        {
            try
            {
                var response = await RequestAsync<TResponse>(cmdMerge);
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 带回调的发送失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法（有请求体，泛型响应）
        /// </summary>
        private async UniTaskVoid SendWithCallbackAsync<TRequest, TResponse>(
            int cmdMerge,
            TRequest request,
            Action<TResponse> callback
        )
            where TRequest : IMessage
            where TResponse : IMessage, new()
        {
            try
            {
                var response = await RequestAsync<TRequest, TResponse>(cmdMerge, request);
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 带回调的发送失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法（有请求体，原始响应）
        /// </summary>
        private async UniTaskVoid SendWithCallbackAsync<TRequest>(
            int cmdMerge,
            TRequest request,
            Action<ResponseMessage> callback
        )
            where TRequest : IMessage
        {
            try
            {
                var response = await RequestAsync(cmdMerge, request);
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 带回调的发送失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法（RequestCommand，原始响应）
        /// </summary>
        private async UniTaskVoid SendWithCallbackAsync(
            RequestCommand command,
            Action<ResponseMessage> callback
        )
        {
            try
            {
                var response = await RequestAsync(command);
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 带回调的发送失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        #endregion

        private void EnsureConnected()
        {
            if (!_connectionManager.IsConnected)
                throw new InvalidOperationException("Not connected to server");
        }
    }
}
