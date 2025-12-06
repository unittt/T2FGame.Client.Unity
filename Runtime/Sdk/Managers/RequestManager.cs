using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using T2FGame.Client.Utils;

namespace T2FGame.Client.Sdk
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
        public async UniTask<ResponseMessage> RequestAsync<TRequest>(
            int cmdMerge,
            TRequest request,
            CancellationToken cancellationToken = default
        )
            where TRequest : IMessage
        {
            EnsureConnected();
            var command = RequestCommand.Of(cmdMerge, request);
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
        /// 发送整数请求
        /// </summary>
        public void SendInt(int cmdMerge, int value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.OfInt(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送字符串请求
        /// </summary>
        public void SendString(int cmdMerge, string value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.OfString(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送长整数请求
        /// </summary>
        public void SendLong(int cmdMerge, long value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.OfLong(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        /// <summary>
        /// 发送布尔值请求
        /// </summary>
        public void SendBool(int cmdMerge, bool value)
        {
            if (!_connectionManager.IsConnected)
                return;

            var command = RequestCommand.OfBool(cmdMerge, value);
            _connectionManager.Client.SendRequest(command);
        }

        #endregion

        #region 带回调的发送 (Callback)

        /// <summary>
        /// 发送请求并在收到响应时执行回调
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
                // 如果没有回调，直接发送不等待响应
                Send(cmdMerge, request);
                return;
            }

            // 异步发送并在收到响应后调用回调
            SendWithCallbackAsync(cmdMerge, request, callback).Forget();
        }

        /// <summary>
        /// 内部异步发送并处理回调的方法
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

        #endregion

        private void EnsureConnected()
        {
            if (!_connectionManager.IsConnected)
                throw new InvalidOperationException("Not connected to server");
        }
    }
}
