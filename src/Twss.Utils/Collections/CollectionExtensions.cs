using System;
using System.Collections.Generic;


namespace Twss.Utils.Collections;


/// <summary>Provides extension methods for collections
/// (<see cref="IEnumerable{T}"/> or derived interfaces/classes).</summary>
public static class CollectionExtensions
{
  /// <summary>Enumerates all combinations with <paramref name="k"/> elements from given <paramref name="source"/>
  /// with n elements (commonly referred to as "n choose k").</summary>
  /// <param name="source">The collection of elements to select all possible combinations from
  /// having <paramref name="k"/> elements.</param>
  /// <param name="k">The number of elements that the combinations to be returned should have.</param>
  /// <param name="elementArray">The array to be used as returned element; must be <c>null</c> or have size
  /// <paramref name="k"/>; when the argument is <c>null</c> then every combination is returned in a new array.</param>
  /// <exception cref="ArgumentException"><paramref name="source"/> is <c>null</c>
  /// - OR - <paramref name="k"/> is less than 0 or greater than the number of <paramref name="source"/> elements
  /// - OR - <paramref name="elementArray"/> is not <c>null</c> but doesn't have <paramref name="k"/>
  /// elements.</exception>
  /// <remarks>
  /// The method has two modes of operation:
  /// <list type="bullet">
  /// <item>When <paramref name="elementArray"/> is <c>null</c> then every combination is returned within a new array.
  /// The returned combinations can be stored or chained with other LINQ invocations.</item>
  /// <item>When <paramref name="elementArray"/> is not <c>null</c> the the given array is reused; every combination
  /// is copied into the given array. Every returned element must be evaluated immediately
  /// (before the array contents are overwritten in the next enumeration step).</item>
  /// </list>
  /// </remarks>
  /// <seealso href="https://en.wikipedia.org/wiki/Binomial_coefficient"/>
  public static IEnumerable<T[]> EnumerateNChooseK<T>(this IReadOnlyList<T> source, int k, T[]? elementArray = null)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));
    if (k < 0 || k > source.Count)
      throw new ArgumentOutOfRangeException(nameof(k));
    if (elementArray != null && elementArray.Length != k)
      throw new ArgumentException(nameof(elementArray));

    // Corner cases => return empty combination.
    if (source.Count <= 0 || k == 0)
    {
      yield return elementArray ?? Array.Empty<T>();
      yield break;
    }

    // Build the combinations to select/return using source indexer; start with [0, 1, ..., k-1] and increment.
    // Returned combinations are expected to be order-invariant
    // => choose combinations where the index values are strictly ascending.
    int[] indexes = new int[k];
    for (int i = 0; i < indexes.Length; i++)
      indexes[i] = i;

    // Initial combination always exists (except for corner cases which are handled already).
    T[] resultElement = elementArray ?? new T[k];
    for (int i = 0; i < indexes.Length; i++)
      resultElement[i] = source[indexes[i]];
    yield return resultElement;

    while (MoveNext(indexes, source.Count))
    {
      resultElement = elementArray ?? new T[k];
      for (int i = 0; i < indexes.Length; i++)
        resultElement[i] = source[indexes[i]];
      yield return resultElement;
    }

    // Nested methods.

    bool MoveNext(int[] indexes, int n)
    {
      var k = indexes.Length;
      var nMinusK = n - k;

      // Traverse array backwards; find first opportunity to increase index.
      for (var i = k - 1; i >= 0; i--)
      {
        var indexAtI = indexes[i];

        // The value must be less than or equal to a value so that the indexes array
        // contains a strictly incremental sequence of integers with a maximum of (n - 1).
        if (indexAtI < nMinusK + i)
        {
          // Increment at found index position;
          // set the rest of the array to the lowest values possible while still being strictly incremental.
          var nextValue = indexAtI + 1;
          for (var j = i; j < k; j++)
          {
            indexes[j] = nextValue;
            nextValue++;
          }
          return true;
        }
      }

      // Every position in indexes is already at the maximum allowed value => done.
      return false;
    }
  }
}
