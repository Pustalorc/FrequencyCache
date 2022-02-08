using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using JetBrains.Annotations;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.Libraries.FrequencyCache;

/// <inheritdoc />
/// <summary>
/// An object that will manage storing and request updates for any cached items.
/// </summary>
/// <typeparam name="T1">The type of the object that will be cached.</typeparam>
/// <typeparam name="T2">The type of the object that will be used as a key for <typeparamref name="T1" />.</typeparam>
[UsedImplicitly]
public class Cache<T1, T2> : IDisposable where T2 : class
{
    /// <summary>
    /// A fixed size array to store all the cached elements.
    /// </summary>
    protected AccessCounter<T1, T2>?[] CachedItems { get; set; }

    /// <summary>
    /// A dictionary for fast lookup of already cached elements.
    /// </summary>
    protected Dictionary<T2, AccessCounter<T1, T2>> CachedItemsIndexed { get; set; }

    /// <summary>
    /// A timer used to periodically request an update to a cached item.
    /// </summary>
    protected Timer? CacheRefreshingTimer { get; set; }

    /// <summary>
    /// The equality comparer used in the underlying dictionary. Stored here as readonly for config reloads.
    /// </summary>
    protected IEqualityComparer<T2> EqualityComparer { get; }

    /// <summary>
    /// A delegate defining the method structure for when a cached item's update is requested.
    /// </summary>
    public delegate void CachedItemUpdateRequested(AccessCounter<T1, T2> item);

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
        EqualityComparer = equalityComparer;
        CachedItems = new AccessCounter<T1, T2>?[configuration.CacheSize];
        CachedItemsIndexed = new Dictionary<T2, AccessCounter<T1, T2>>(configuration.CacheSize, equalityComparer);

        if (!configuration.EnableCacheRefreshes) return;

        CacheRefreshingTimer = new Timer(configuration.CacheRefreshRequestInterval);
        CacheRefreshingTimer.Elapsed += RequestCacheRefresh;
        CacheRefreshingTimer.Start();
    }

    /// <summary>
    /// Reloads the cache with a new configuration file.
    /// </summary>
    /// <param name="configuration">The new configuration file to use to reload the cache.</param>
    /// <remarks>
    /// This will reset the cached items to a new array, and the same for the indexed dictionary.
    /// Timer will also be updated as necessary (removed if disabled, created if enabled but was disabled before, and updated if enabled and was enabled before).
    /// </remarks>
    [UsedImplicitly]
    public virtual void ReloadConfiguration(ICacheConfiguration configuration)
    {
        CachedItems = new AccessCounter<T1, T2>?[configuration.CacheSize];
        CachedItemsIndexed = new Dictionary<T2, AccessCounter<T1, T2>>(configuration.CacheSize, EqualityComparer);

        if (!configuration.EnableCacheRefreshes)
        {
            CacheRefreshingTimer?.Stop();
            CacheRefreshingTimer?.Dispose();
            CacheRefreshingTimer = null;
            return;
        }

        if (CacheRefreshingTimer == null)
        {
            CacheRefreshingTimer = new Timer(configuration.CacheRefreshRequestInterval);
            CacheRefreshingTimer.Elapsed += RequestCacheRefresh;
            CacheRefreshingTimer.Start();
        }
        else
        {
            CacheRefreshingTimer.Stop();
            CacheRefreshingTimer.Interval = configuration.CacheRefreshRequestInterval;
            CacheRefreshingTimer.Start();
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        CacheRefreshingTimer?.Stop();
        CacheRefreshingTimer?.Dispose();
    }

    /// <summary>
    /// Gets an item from cache based on its specified key.
    /// </summary>
    /// <param name="key">The key to use as search to get the item in cache.</param>
    /// <returns>
    /// <see langword="null"/> if there's no element with the key.
    /// <see cref="AccessCounter{T1,T2}"/> if there's an element with the key.
    /// </returns>
    [UsedImplicitly]
    public virtual AccessCounter<T1, T2>? GetItemInCache(T2 key)
    {
        return !CachedItemsIndexed.TryGetValue(key, out var cachedItem)
            ? null
            : cachedItem;
    }

    /// <summary>
    /// Stores or updates an item into/in cache based on its specified key.
    /// </summary>
    /// <param name="key">The key to use to search or store the item into cache.</param>
    /// <param name="item">The item to store in cache.</param>
    /// <returns>
    /// An instance of <see cref="AccessCounter{T1,T2}"/>, containing the item that is cached with its key.
    /// </returns>
    /// <remarks>
    /// Despite the fact that the key should technically be fully unique per item, this is not a restriction here that is imposed.
    /// </remarks>
    [UsedImplicitly]
    public virtual AccessCounter<T1, T2> StoreUpdateItem(T2 key, T1 item)
    {
        var itemInCache = GetItemInCache(key);
        if (itemInCache != null)
        {
            itemInCache.Item = item;
            return itemInCache;
        }

        var index = GetBestCacheIndex();

        var oldItem = CachedItems[index];
        if (oldItem != null)
            CachedItemsIndexed.Remove(oldItem.Key);

        var newItem = new AccessCounter<T1, T2>(item, key);
        CachedItems[index] = newItem;
        CachedItemsIndexed.Add(key, newItem);

        return newItem;
    }

    /// <summary>
    /// Retrieves the best possible index for a new element to be added to cache.
    /// </summary>
    /// <returns>
    /// An integer, which is the index on the internal array to use for the new element.
    /// </returns>
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