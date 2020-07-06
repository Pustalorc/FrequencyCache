using System;
using System.Linq;
using System.Timers;

namespace Pustalorc.Libraries.FrequencyCache
{
    /// <summary>
    /// The main system that caches and sends update requests.
    /// </summary>
    public sealed class CacheManager
    {
        private readonly CachedItem[] m_CachedItems;
        private readonly Timer m_RequestUpdates;
        
        public delegate void UpdateCacheItem(CachedItem item);
        public event UpdateCacheItem OnCachedItemUpdateRequested;

        public CacheManager(ICacheConfiguration configuration)
        {
            m_CachedItems = new CachedItem[configuration.CacheSize];
            
            m_RequestUpdates = new Timer(configuration.CacheRefreshRequestInterval);
            m_RequestUpdates.Elapsed += RequestCacheRefresh;
            m_RequestUpdates.Start();
        }

        public CachedItem GetItemInCache(IIdentifiable identifiable)
        {
            return m_CachedItems.FirstOrDefault(k =>
                k.Identity.Equals(identifiable.UniqueIdentifier, StringComparison.OrdinalIgnoreCase));
        }

        public void StoreUpdateItem(IIdentifiable identifiable)
        {
            var cache = GetItemInCache(identifiable);
            if (cache != null)
                cache.Identifiable = identifiable;
            else
                m_CachedItems[GetBestCacheIndex()] = new CachedItem(identifiable);
        }
        
        public void ModifyCacheRefreshInterval(double time)
        {
            m_RequestUpdates.Stop();
            m_RequestUpdates.Interval = time;
            m_RequestUpdates.Start();
        }
        
        private int GetBestCacheIndex()
        {
            var index = m_CachedItems.FindFirstIndexNull();
            if (index > -1)
                return index;

            var toReplace = m_CachedItems.Where(k => k != null).OrderByDescending(k => k.Weight).FirstOrDefault();

            return toReplace == null
                ? m_CachedItems.FindFirstIndexNull()
                : m_CachedItems.FindFirstIndex(k =>
                    k?.Identity.Equals(toReplace.Identity, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void RequestCacheRefresh(object sender, ElapsedEventArgs e)
        {
            foreach (var element in m_CachedItems)
                OnCachedItemUpdateRequested?.Invoke(element);
        }
    }
}