using System.Collections.Generic;
using System.Linq;
using System.Timers;
using JetBrains.Annotations;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.Libraries.FrequencyCache
{
    /// <summary>
    /// An object that will manage storing and request updates for any cached items.
    /// </summary>
    /// <typeparam name="T1">The type of the object that will be cached.</typeparam>
    /// <typeparam name="T2">The type of the object that will be used as a key for <typeparamref name="T1"/>.</typeparam>
    [UsedImplicitly]
    public class Cache<T1, T2> where T2 : class
    {
        /// <summary>
        /// A fixed size array to store all the cached elements.
        /// </summary>
        protected readonly AccessCounter<T1>?[] CachedItems;

        /// <summary>
        /// A dictionary for fast lookup of already cached elements.
        /// </summary>
        protected readonly Dictionary<T2, int> CachedItemsIndexed;

        /// <summary>
        /// A dictionary for fast lookup of the key of a cached element.
        /// </summary>
        protected readonly Dictionary<int, T2> IndexToKey;

        /// <summary>
        /// A timer used to periodically request an update to a cached item.
        /// </summary>
        protected readonly Timer? CacheRefreshingTimer;

        /// <summary>
        /// A delegate defining the method structure for when a cached item's update is requested.
        /// </summary>
        public delegate void CachedItemUpdateRequested(AccessCounter<T1> item);

        /// <summary>
        /// An event for requesting a cached item's update.
        /// </summary>
        [UsedImplicitly]
        public event CachedItemUpdateRequested? OnCachedItemUpdateRequested;

        /// <summary>
        /// Constructs a new instance of the cache.
        /// </summary>
        /// <param name="configuration">The configuration to use for this cache. It normally is not stored.</param>
        /// <param name="equalityComparer">The equality comparer for the keys in the internal indexed dictionary.</param>
        public Cache(ICacheConfiguration configuration, IEqualityComparer<T2> equalityComparer)
        {
            CachedItems = new AccessCounter<T1>?[configuration.CacheSize];
            CachedItemsIndexed = new Dictionary<T2, int>(configuration.CacheSize, equalityComparer);
            IndexToKey = new Dictionary<int, T2>(configuration.CacheSize);

            if (!configuration.EnableCacheRefreshes) return;

            CacheRefreshingTimer = new Timer(configuration.CacheRefreshRequestInterval);
            CacheRefreshingTimer.Elapsed += RequestCacheRefresh;
            CacheRefreshingTimer.Start();
        }

        /// <summary>
        /// Gets an item from cache based on a key input.
        /// </summary>
        /// <param name="key">The key to use as search to get the item in cache.</param>
        /// <returns>
        /// <see langword="null"/> if there's no element with the key.
        /// <see cref="AccessCounter{T1}"/> if there's an element with the key.
        /// </returns>
        [UsedImplicitly]
        public virtual AccessCounter<T1>? GetItemInCache(T2 key)
        {
            return !CachedItemsIndexed.TryGetValue(key, out var cachedItemIndex)
                ? null
                : CachedItems[cachedItemIndex];
        }

        /// <summary>
        /// Stores or updates an item into cache based on a key.
        /// </summary>
        /// <param name="key">The key to use to search or store the item into cache.</param>
        /// <param name="item">The item to store in cache.</param>
        /// <returns>
        /// An instance of <see cref="AccessCounter{T1}"/>, containing the item that is cached.
        /// </returns>
        [UsedImplicitly]
        public virtual AccessCounter<T1> StoreUpdateItem(T2 key, T1 item)
        {
            var itemInCache = GetItemInCache(key);
            if (itemInCache != null)
            {
                itemInCache.Item = item;
                return itemInCache;
            }

            var index = GetBestCacheIndex();

            var newItem = new AccessCounter<T1>(item);
            CachedItems[index] = newItem;

            if (IndexToKey.TryGetValue(index, out var oldKey))
            {
                CachedItemsIndexed.Remove(oldKey);
                IndexToKey[index] = key;
            }
            else
            {
                IndexToKey.Add(index, key);
            }

            CachedItemsIndexed.Add(key, index);

            return newItem;
        }

        /// <summary>
        /// Restarts the internal timer with a new interval.
        /// </summary>
        /// <param name="rate">The new interval for the timer to tick at.</param>
        /// <returns>A boolean to determine if changing the interval was successful.</returns>
        /// <remarks>
        /// This will always return false if the RequestUpdates property is null. Only way to make it return true is on
        /// Instantiation by providing a configuration that enables cache refreshes.
        /// </remarks>
        [UsedImplicitly]
        public virtual bool ModifyCacheRefreshInterval(double rate)
        {
            if (CacheRefreshingTimer == null)
                return false;

            CacheRefreshingTimer.Stop();
            CacheRefreshingTimer.Interval = rate;
            CacheRefreshingTimer.Start();
            return true;
        }

        /// <summary>
        /// Gets the best index for a new item to be cached at.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetBestCacheIndex()
        {
            var cacheList = CachedItems.ToList();

            var toReplace = cacheList.OrderByDescending(k => k?.Weight ?? double.MaxValue).First();

            if (toReplace == null)
                return cacheList.FindIndex(k => k == null);

            var index = cacheList.IndexOf(toReplace);

            return index;
        }

        private void RequestCacheRefresh(object? sender, ElapsedEventArgs e)
        {
            foreach (var element in CachedItems.Where(element => element != null))
                OnCachedItemUpdateRequested?.Invoke(element!);
        }
    }
}