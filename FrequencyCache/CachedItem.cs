using System;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.Libraries.FrequencyCache
{
    /// <summary>
    /// An element that has been cached into memory.
    /// </summary>
    public sealed class CachedItem
    {
        /// <summary>
        /// Internal identifiable element, to be used only for bypassing the access counter.
        /// </summary>
        internal IIdentifiable ModifiableIdentifiable;

        /// <summary>
        /// Accesses the cached element, and increases access count, last access time and average time between accesses.
        /// For best practice, temporarily store the identifiable in a local variable if accessed more than once in a
        /// single method.
        /// </summary>
        public IIdentifiable Identifiable
        {
            get
            {
                Accessed();
                return ModifiableIdentifiable;
            }
            set
            {
                Accessed();
                ModifiableIdentifiable = value;
            }
        }

        /// <summary>
        /// The unique identifier from the identifiable. Bypasses the access counter.
        /// </summary>
        public string Identity => ModifiableIdentifiable.UniqueIdentifier;

        /// <summary>
        /// The weight of this element in cache. Lower is better.
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
        /// The number of times the identifiable has been accessed.
        /// </summary>
        public ulong AccessCount { get; private set; }

        /// <summary>
        /// The average time in milliseconds between each access.
        /// </summary>
        public double AverageTimeBetweenAccesses { get; private set; }

        /// <summary>
        /// The last time the identifiable was accessed.
        /// </summary>
        public DateTime LastAccess { get; private set; }

        /// <summary>
        /// The time that the identifiable was stored in cache.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// Creates a new cached item that stores an identifiable.
        /// </summary>
        /// <param name="identifiable">The identifiable to be stored.</param>
        public CachedItem(IIdentifiable identifiable)
        {
            Identifiable = identifiable;
            Created = DateTime.Now;
            LastAccess = DateTime.Now;
            AverageTimeBetweenAccesses = 0;
            AccessCount = 0;
        }

        /// <summary>
        /// Access counter that increases the access count and the average time between accesses.
        /// Also updates last accessed time.
        /// </summary>
        private void Accessed()
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