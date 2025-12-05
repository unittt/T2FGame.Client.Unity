namespace T2FGame.Client.Utils
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的对象可以在对象池中正确地重置状态
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象从池中取出时调用
        /// 用于初始化或重置对象状态
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 当对象归还到池中时调用
        /// 用于清理对象状态，释放引用
        /// </summary>
        void OnDespawn();
    }
}
