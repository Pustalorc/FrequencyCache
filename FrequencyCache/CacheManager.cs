using System;
using System.Linq;
using System.Timers;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.Libraries.FrequencyCache
{
    /// <summary>
    /// The main system that caches and sends update requests.
    /// </summary>
    public sealed class CacheManager
    {
        private readonly CachedItem[] m_CachedItems;
        private readonly Timer m_RequestUpdates;

        public delegate void UpdateCacheItem(CachedItem item, IIdentifiable identifiable);

        /// <summary>
        /// Event raised whenever an element in cache is requested to be updated.
        /// </summary>
        public event UpdateCacheItem OnCachedItemUpdateRequested;

        /// <summary>
        /// Creates a new cache with the provided configuration.
        /// </summary>
        /// <param name="configuration">The configuration to be used on the manager.</param>
        public CacheManager(ICacheConfiguration configuration)
        {
            m_CachedItems = new CachedItem[configuration.CacheSize];

            m_RequestUpdates = new Timer(configuration.CacheRefreshRequestInterval);
            m_RequestUpdates.Elapsed += RequestCacheRefresh;
            m_RequestUpdates.Start();
        }

        /// <summary>
        /// Retrieves an element from cache that matches the provided identifiable.
        /// </summary>
        /// <param name="identifiable">The identifiable to match to.</param>
        /// <returns></returns>
        public CachedItem GetItemInCache(IIdentifiable identifiable)
        {
            return m_CachedItems.FirstOrDefault(k =>
                k.Identity.Equals(identifiable.UniqueIdentifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Stores a new identifiable on an available index, or if it is found in cache, it updates it without modifying the access count.
        /// </summary>
        /// <param name="identifiable">The identifiable to store or update in cache.</param>
        public void StoreUpdateItem(IIdentifiable identifiable)
        {
            if (m_CachedItems.Length == 0) return;

            var cache = GetItemInCache(identifiable);
            if (cache != null)
                cache.ModifiableIdentifiable = identifiable;
            else
                m_CachedItems[GetBestCacheIndex()] = new CachedItem(identifiable);
        }

        /// <summary>
        /// Modifies the cache refresh interval to the specified rate in milliseconds.
        /// </summary>
        /// <param name="rate">The rate to be updated to in milliseconds</param>
        public void ModifyCacheRefreshInterval(double rate)
        {
            m_RequestUpdates.Stop();
            m_RequestUpdates.Interval = rate;
            m_RequestUpdates.Start();
        }

        /// <summary>
        /// Retrieve the best index of the cache to replace or use for a new element.
        /// </summary>
        /// <returns>An index of m_CachedItems.</returns>
        private int GetBestCacheIndex()
        {
            var cacheList = m_CachedItems.ToList();

            var index = cacheList.FindIndex(k => k == null);
            if (index > -1)
                return index;

            var toReplace = m_CachedItems.Where(k => k != null).OrderByDescending(k => k.Weight).FirstOrDefault();

            return toReplace == null
                ? cacheList.FindIndex(k => k == null)
                : cacheList.FindIndex(k =>
                    k?.Identity.Equals(toReplace.Identity, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void RequestCacheRefresh(object sender, ElapsedEventArgs e)
        {
            foreach (var element in m_CachedItems)
                OnCachedItemUpdateRequested?.Invoke(element, element.ModifiableIdentifiable);
        }
    }
}