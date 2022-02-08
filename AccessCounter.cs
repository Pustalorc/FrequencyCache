using System;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.FrequencyCache;

/// <summary>
/// A class that wraps any object <typeparamref name="T1"/> in a class and tracks how many times the object is accessed.
/// Also utilizes <typeparamref name="T2"/> as to internally track what identifies this object from the others for when doing a dictionary search.
/// </summary>
/// <typeparam name="T1">The type of the object that will be wrapped.</typeparam>
/// <typeparam name="T2">The type of the identifying key for this object.</typeparam>
/// <remarks>
/// <typeparamref name="T2"/> can be the same as T1, but is highly discouraged.
/// </remarks>
[UsedImplicitly]
public class AccessCounter<T1, T2>
{
    /// <summary>
    /// A property to access the uniquely identifiable key <typeparamref name="T2"/> for this object.
    /// </summary>
    public T2 Key { get; }

    /// <summary>
    /// A field to access the wrapped <typeparamref name="T1"/> without counting as a normal access.
    /// </summary>
    protected T1 DirectAccessItem;

    /// <summary>
    /// A property to access the wrapped <typeparamref name="T1"/> and counting the access.
    /// </summary>
    [UsedImplicitly]
    public T1 Item
    {
        get
        {
            Accessed();
            return DirectAccessItem;
        }
        set
        {
            Accessed();
            DirectAccessItem = value;
        }
    }

    /// <summary>
    /// The number of times this object has been accessed.
    /// </summary>
    public ulong AccessCount { get; protected set; }

    /// <summary>
    /// The average amount of time between accesses in milliseconds.
    /// </summary>
    public double AverageTimeBetweenAccesses { get; protected set; }

    /// <summary>
    /// The last time the object was accessed.
    /// </summary>
    public long LastAccess { get; protected set; }

    /// <summary>
    /// The time the wrapper was created.
    /// </summary>
    public long Created { get; }

    /// <summary>
    /// Constructs a new wrapper for a new item.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    /// <param name="key">The uniquely identifiable key for this object.</param>
    public AccessCounter(T1 item, T2 key)
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Created = unixTimestamp;
        LastAccess = unixTimestamp;
        AverageTimeBetweenAccesses = 0;
        AccessCount = 0;
        DirectAccessItem = item;
        Key = key;
    }

    /// <summary>
    /// The current weight (importance level) of the wrapped item.
    /// High value = Important, gets accessed a lot and frequently.
    /// Low Value = Not Important, gets accessed a little and infrequently.
    /// </summary>
    public virtual double Weight
    {
        get
        {
            var denominator = AccessCount * AverageTimeBetweenAccesses;

            if (denominator == 0)
                return 0;

            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timeSinceCreation = unixTimestamp - Created;
            var timeSinceAccess = unixTimestamp - LastAccess;

            return timeSinceCreation * timeSinceAccess / denominator;
        }
    }

    /// <summary>
    /// Triggers a recalculation and change of all properties tracking the access of the wrapped object.
    /// </summary>
    protected virtual void Accessed()
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var oldSum = AverageTimeBetweenAccesses * AccessCount;

        AccessCount++;

        oldSum += unixTimestamp - LastAccess;
        AverageTimeBetweenAccesses = oldSum / AccessCount;

        LastAccess = unixTimestamp;
    }
}