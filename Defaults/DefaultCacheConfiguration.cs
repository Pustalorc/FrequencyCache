using JetBrains.Annotations;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.Libraries.FrequencyCache.Defaults;

/// <inheritdoc />
/// <summary>
/// A default example configuration for the cache.
/// </summary>
[UsedImplicitly]
public class DefaultCacheConfiguration : ICacheConfiguration
{
    /// <inheritdoc />
    public bool EnableCacheRefreshes => true;

    /// <inheritdoc />
    public virtual double CacheRefreshRequestInterval => 30000;

    /// <inheritdoc />
    public virtual int CacheSize => 125;
}