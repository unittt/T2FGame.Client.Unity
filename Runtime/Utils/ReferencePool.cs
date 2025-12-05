using UnityEngine.Pool;

namespace T2FGame.Client.Utils
{
    /// <summary>
    /// 引用对象池
    /// 提供高效的对象复用机制，减少 GC 压力
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    public static class ReferencePool<T> where T : class, new()
    {
        private static readonly ObjectPool<T> _pool;

        static ReferencePool()
        {
            _pool = new ObjectPool<T>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 1000
            );
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public static T Spawn() => _pool.Get();

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        /// <param name="item">要归还的对象</param>
        public static void Despawn(T item)
        {
            if (item == null) return;
            _pool.Release(item);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public static void Clear() => _pool.Clear();

        /// <summary>
        /// 获取当前池中的对象数量
        /// </summary>
        public static int CountInactive => _pool.CountInactive;

        private static T CreateItem() => new();

        private static void OnGet(T item)
        {
            // 如果对象实现了 IPoolable 接口，调用 OnSpawn
            if (item is IPoolable poolable)
            {
                poolable.OnSpawn();
            }
        }

        private static void OnRelease(T item)
        {
            // 如果对象实现了 IPoolable 接口，调用 OnDespawn
            if (item is IPoolable poolable)
            {
                poolable.OnDespawn();
            }
        }

        private static void OnDestroy(T item)
        {
            // 如果对象实现了 System.IDisposable，释放资源
            if (item is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
