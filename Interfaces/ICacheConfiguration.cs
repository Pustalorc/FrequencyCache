namespace Pustalorc.Libraries.FrequencyCache.Interfaces
{
    /// <summary>
    /// The interface to define any class as a valid configuration for the cache.
    /// </summary>
    public interface ICacheConfiguration
    {
        /// <summary>
        /// The interval between a complete cache refresh request.
        /// </summary>
        public double CacheRefreshRequestInterval { get; }

        /// <summary>
        /// The maximum size of the cache, not per element, but total number of elements.
        /// </summary>
        public int CacheSize { get; }
    }
}