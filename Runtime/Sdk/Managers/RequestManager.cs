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
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        #region 异步请求 (Async)

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(int cmdMerge, CancellationToken cancellationToken = default)
        {
            return await RequestAsync(RequestCommand.Of(cmdMerge),  cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync<TRequest>(int cmdMerge, TRequest request, CancellationToken cancellationToken = default) where TRequest : IMessage
        {
            return await RequestAsync(RequestCommand.Of(cmdMerge, request), cancellationToken);
        }

        /// <summary>
        /// 直接发送 RequestCommand 并等待响应(统一的异步请求入口)
        /// </summary>
        public async UniTask<ResponseMessage> RequestAsync(RequestCommand command, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            return await _connectionManager.Client.RequestAsync(command, cancellationToken);
        }

        /// <summary>
        /// 发送请求并等待响应（获取指定类型的响应数据）
        /// </summary>
        public async UniTask<TResponse> RequestAsync<TResponse>(int cmdMerge, CancellationToken cancellationToken = default) where TResponse : IMessage, new()
        {
            var response = await RequestAsync(cmdMerge, cancellationToken);
            ThrowIfError(response);
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
            ThrowIfError(response);
            return response.GetValue<TResponse>();
        }

        #endregion

        #region 发送不等待响应 (Fire and Forget)

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send(int cmdMerge)
            => Send(RequestCommand.Of(cmdMerge));

        /// <summary>
        /// 发送请求（仅发送，不等待响应）
        /// </summary>
        public void Send<TRequest>(int cmdMerge, TRequest request) where TRequest : IMessage
            => Send(RequestCommand.Of(cmdMerge, request));


        /// <summary>
        /// 统一的发送入口
        /// </summary>
        public void Send(RequestCommand command)
        {
            _connectionManager.Client.Send(command);
        }

        #endregion

        #region 带回调的发送 (Callback)

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，原始响应）
        /// </summary>
        public void Send(int cmdMerge, Action<ResponseMessage> callback)
            => ExecuteWithCallback(() => RequestAsync(cmdMerge), callback, () => Send(cmdMerge));

        /// <summary>
        /// 发送请求并在收到响应时执行回调（无请求体，泛型响应）
        /// </summary>
        public void Send<TResponse>(int cmdMerge, Action<TResponse> callback)
            where TResponse : IMessage, new()
            => ExecuteWithCallback(() => RequestAsync<TResponse>(cmdMerge), callback, () => Send(cmdMerge));

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，泛型响应）
        /// </summary>
        public void Send<TRequest, TResponse>(int cmdMerge, TRequest request, Action<TResponse> callback) where TRequest : IMessage where TResponse : IMessage, new()
            => ExecuteWithCallback
            (
                () => RequestAsync<TRequest, TResponse>(cmdMerge, request),
                callback,
                () => Send(cmdMerge, request)
            );

        /// <summary>
        /// 发送请求并在收到响应时执行回调（有请求体，原始响应）
        /// </summary>
        public void Send<TRequest>(int cmdMerge, TRequest request, Action<ResponseMessage> callback)
            where TRequest : IMessage
            => ExecuteWithCallback(
                () => RequestAsync(cmdMerge, request),
                callback,
                () => Send(cmdMerge, request)
            );

        /// <summary>
        /// 直接发送 RequestCommand
        /// </summary>
        public void Send(RequestCommand command, Action<ResponseMessage> callback)
        {
            ExecuteWithCallback(() => RequestAsync(command), callback, () => Send(command));
        }

        /// <summary>
        /// 统一的回调执行入口
        /// </summary>
        private void ExecuteWithCallback<T>(Func<UniTask<T>> requestFunc, Action<T> callback, Action fallbackSend)
        {
            if (!_connectionManager.IsConnected)
            {
                GameLogger.LogWarning("[RequestManager] 未连接，无法发送请求");
                PiscesClientCode.NotConnected.ThrowIfNotSuccess();
            }

            if (callback == null)
            {
                fallbackSend?.Invoke();
                return;
            }

            ExecuteWithCallbackAsync(requestFunc, callback).Forget();
        }

        /// <summary>
        /// 统一的异步回调执行
        /// </summary>
        private async UniTaskVoid ExecuteWithCallbackAsync<T>(Func<UniTask<T>> requestFunc, Action<T> callback)
        {
            try
            {
                var response = await requestFunc();
                callback?.Invoke(response);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[RequestManager] 请求失败: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        #endregion

        #region 辅助方法

        private void EnsureConnected()
        {
            if (!_connectionManager.IsConnected)
                PiscesClientCode.NotConnected.ThrowIfNotSuccess();
        }

        private static void ThrowIfError(ResponseMessage response)
        {
            if (response.HasError)
                throw new PiscesServerException(response);
        }

        #endregion
    }
}
