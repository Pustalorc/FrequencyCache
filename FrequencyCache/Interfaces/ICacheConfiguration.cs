namespace Pustalorc.Libraries.FrequencyCache.Interfaces
{
    /// <summary>
    /// The basic structure for a class to configure the cache.
    /// </summary>
    public interface ICacheConfiguration
    {
        /// <summary>
        /// The minimum time in milliseconds between each cache refresh request.
        /// </summary>
        double CacheRefreshRequestInterval { get; }

        /// <summary>
        /// The maximum amount of elements the cache should hold.
        /// </summary>
        ulong CacheSize { get; }
    }
}