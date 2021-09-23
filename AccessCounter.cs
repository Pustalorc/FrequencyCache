using System;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.FrequencyCache
{
    /// <summary>
    /// A class that wraps any object <typeparamref name="T"/> in a class and tracks how many times the object is accessed.
    /// </summary>
    /// <typeparam name="T">The type of the object that will be wrapped.</typeparam>
    [UsedImplicitly]
    public class AccessCounter<T>
    {
        /// <summary>
        /// A field to access the wrapped <typeparamref name="T"/> without counting as a normal access.
        /// </summary>
        protected T DirectAccessItem;

        /// <summary>
        /// A property to access the wrapped <typeparamref name="T"/> and counting the access.
        /// </summary>
        [UsedImplicitly]
        public T Item
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
        /// The current weight (importance level) of the wrapped item.
        /// High value = Important, gets accessed a lot and frequently.
        /// Low Value = Not Important, gets accessed a little and infrequently.
        /// </summary>
        public double Weight
        {
            get
            {
                var now = DateTime.Now;
                var timeSinceCreation = now.Subtract(Created).TotalMilliseconds;
                var timeSinceAccess = now.Subtract(LastAccess).TotalMilliseconds;
                var denominator = AccessCount * AverageTimeBetweenAccesses;

                if (denominator == 0)
                    return 0;

                return timeSinceCreation * timeSinceAccess / denominator;
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
        public DateTime LastAccess { get; protected set; }

        /// <summary>
        /// The time the wrapper was created.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// Constructs a new wrapper for a new item.
        /// </summary>
        /// <param name="item">The item to wrap.</param>
        public AccessCounter(T item)
        {
            Created = DateTime.Now;
            LastAccess = DateTime.Now;
            AverageTimeBetweenAccesses = 0;
            AccessCount = 0;
            DirectAccessItem = item;
        }

        /// <summary>
        /// Triggers a recalculation and change of all properties tracking the access of the wrapped object.
        /// </summary>
        protected virtual void Accessed()
        {
            var now = DateTime.Now;
            var oldSum = AverageTimeBetweenAccesses * AccessCount;

            AccessCount++;

            oldSum += now.Subtract(LastAccess).TotalMilliseconds;
            AverageTimeBetweenAccesses = oldSum / AccessCount;

            LastAccess = DateTime.Now;
        }
    }
}