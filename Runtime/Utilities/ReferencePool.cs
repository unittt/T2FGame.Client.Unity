using UnityEngine.Pool;

namespace T2FGame.Client.Utilities
{
    /// <summary>
    ///  引用对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ReferencePool<T> where T : class, new()
    {
        private static readonly ObjectPool<T> _pool;
        
        static ReferencePool()
        {
            _pool = new ObjectPool<T>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: null,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 1000
            );
        }

        public static T Spawn() => _pool.Get();
        
        public static void Despawn(T item) => _pool.Release(item);
        
        public static void Clear() => _pool.Clear();

        private static T CreateItem() => new();
        
        private static void OnGet(T item) 
        {
            
        }
        
        private static void OnRelease(T item) 
        {
            
        }
    }
}