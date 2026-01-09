using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Pisces.Protocol
{
    /// <summary>
    /// 集合转换核心辅助类，提供通用的 no-GC 转换方法
    /// </summary>
    public static class CollectionConvertHelper
    {
        /// <summary>
        /// 核心填充引擎：将任何 IList 转换为 List (支持转换器)
        /// </summary>
        public static void FillList<TSource, TResult>(IList<TSource> source, List<TResult> result, Func<TSource, TResult> converter, bool skipPbErrors = false)
        {
            if (result == null) return;
            result.Clear();
            
            if (source == null) return;
            var count = source.Count; // 性能优化：缓存 Count
            if (count == 0) return;

            // 预分配容量，防止 List 内部多次扩容
            if (result.Capacity < count) result.Capacity = count;

            for (var i = 0; i < count; i++)
            {
                if (skipPbErrors)
                {
                    try { AddItem(result, converter(source[i])); }
                    catch (InvalidProtocolBufferException) { }
                }
                else
                {
                    AddItem(result, converter(source[i]));
                }
            }
        }

        /// <summary>
        /// 核心填充引擎：将 IList 转换为 RepeatedField
        /// </summary>
        public static void FillRepeatedField<TSource, TResult>(
            IList<TSource> source, 
            RepeatedField<TResult> result, 
            Func<TSource, TResult> converter)
        {
            if (result == null) return;
            result.Clear();
            
            if (source == null) return;
            var count = source.Count;
            if (count == 0) return;

            result.Capacity = count;
            for (var i = 0; i < count; i++)
            {
                var item = converter(source[i]);
                if (item != null) result.Add(item);
            }
        }

        /// <summary>
        /// 核心填充引擎：将 Entry 列表转换为 Dictionary
        /// </summary>
        public static void FillDictionary<TEntry, TKey, TValue>(
            RepeatedField<TEntry> entries,
            Dictionary<TKey, TValue> result,
            Func<TEntry, TKey> keySelector,
            Func<TEntry, TValue> valueConverter)
        {
            if (result == null) return;
            result.Clear();
            
            if (entries == null) return;
            var count = entries.Count;
            if (count == 0) return;

            for (var i = 0; i < count; i++)
            {
                try
                {
                    var entry = entries[i];
                    var val = valueConverter(entry);
                    if (val != null) result[keySelector(entry)] = val;
                }
                catch (InvalidProtocolBufferException) { }
                catch (ArgumentException) { } // 键冲突处理
            }
        }

        /// <summary>
        /// 核心填充引擎：将 Dictionary 转换为 RepeatedField (Entry 列表)
        /// </summary>
        /// <typeparam name="TKey">字典键类型</typeparam>
        /// <typeparam name="TValue">字典值类型</typeparam>
        /// <typeparam name="TEntry">Entry 类型</typeparam>
        /// <param name="source">源字典</param>
        /// <param name="result">目标 RepeatedField</param>
        /// <param name="entryCreator">Entry 创建器，接收 key 和 value，返回 Entry 对象</param>
        public static void FillEntriesFromDictionary<TKey, TValue, TEntry>(
            IDictionary<TKey, TValue> source,
            RepeatedField<TEntry> result,
            Func<TKey, TValue, TEntry> entryCreator)
        {
            if (result == null) return;
            result.Clear();
            
            if (source == null) return;
            var count = source.Count;
            if (count == 0) return;

            result.Capacity = count;
            foreach (var kvp in source)
            {
                var entry = entryCreator(kvp.Key, kvp.Value);
                if (entry != null) result.Add(entry);
            }
        }

        private static void AddItem<T>(List<T> list, T item)
        {
            if (item != null) list.Add(item);
        }
    }
}
