using System;
using System.Collections.Generic;
using System.Linq;

namespace Pustalorc.Libraries.FrequencyCache
{
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     Returns the index of the first element which satisfies the match within the enumerable.
        /// </summary>
        /// <typeparam name="TSource">The type of the array.</typeparam>
        /// <param name="source">The instance of the enumerable.</param>
        /// <param name="match">The predicate to match the source with.</param>
        /// <returns>An index based on the rules of List.FindIndex</returns>
        public static int FindFirstIndex<TSource>(this IEnumerable<TSource> source, Predicate<TSource> match)
        {
            return source.ToList().FindIndex(match);
        }

        /// <summary>
        ///     Returns the index of the first element which is null within the array.
        /// </summary>
        /// <typeparam name="TSource">A class that can be nullable and that defines the type of the array.</typeparam>
        /// <param name="source">The instance of the array that the first null case should be found.</param>
        /// <returns>An index based on the rules of List.FindIndex</returns>
        public static int FindFirstIndexNull<TSource>(this IEnumerable<TSource> source) where TSource : class
        {
            return source.FindFirstIndex(k => k == null);
        }
    }
}