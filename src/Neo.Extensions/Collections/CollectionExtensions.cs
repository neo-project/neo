// Copyright (C) 2015-2025 The Neo Project.
//
// CollectionExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using Neo.Factories;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Collections;

public static class CollectionExtensions
{
    /// <summary>
    /// Removes the key-value pairs from the dictionary that match the specified predicate.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dict">The dictionary to remove key-value pairs from.</param>
    /// <param name="predicate">The predicate to match key-value pairs.</param>
    /// <param name="afterRemoved">An action to perform after each key-value pair is removed.</param>
    public static void RemoveWhere<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        Func<KeyValuePair<TKey, TValue>, bool> predicate,
        Action<KeyValuePair<TKey, TValue>>? afterRemoved = null)
    {
        var items = new List<KeyValuePair<TKey, TValue>>();
        foreach (var item in dict) // avoid linq
        {
            if (predicate(item))
                items.Add(item);
        }

        foreach (var item in items)
        {
            if (dict.Remove(item.Key))
                afterRemoved?.Invoke(item);
        }
    }

    /// <summary>
    /// Chunks the source collection into chunks of the specified size.
    /// For example, if the source collection is [1, 2, 3, 4, 5] and the chunk size is 3, the result will be [[1, 2, 3], [4, 5]].
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="source">The collection to chunk.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <returns>An enumerable of arrays, each containing a chunk of the source collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the source collection is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the chunk size is less than or equal to 0.</exception>
    public static IEnumerable<T[]> Chunk<T>(this IReadOnlyCollection<T>? source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must > 0.");

        using IEnumerator<T> enumerator = source.GetEnumerator();
        for (var remain = source.Count; remain > 0;)
        {
            var chunk = new T[Math.Min(remain, chunkSize)];
            for (var i = 0; i < chunk.Length; i++)
            {
                if (!enumerator.MoveNext()) // Additional checks
                    throw new InvalidOperationException("unexpected end of sequence");
                chunk[i] = enumerator.Current;
            }

            remain -= chunk.Length;
            yield return chunk;
        }
    }

    /// <summary>
    /// Randomly samples a specified number of elements from the collection using reservoir sampling algorithm.
    /// This method ensures each element has an equal probability of being selected, regardless of the collection size.
    /// If the count is greater than the collection size, the entire collection will be returned.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="collection">The collection to sample from.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>An array containing the randomly sampled elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the collection is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is less than 0</exception>
    [return: NotNull]
    public static T[] Sample<T>(this IReadOnlyCollection<T> collection, int count)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));

        if (count == 0) return [];

        var reservoir = new T[Math.Min(count, collection.Count)];
        var currentIndex = 0;
        foreach (var item in collection)
        {
            if (currentIndex < reservoir.Length)
            {
                reservoir[currentIndex] = item;
            }
            else
            {
                var randomIndex = RandomNumberFactory.NextInt32(0, currentIndex + 1);
                if (randomIndex < reservoir.Length) reservoir[randomIndex] = item;
            }
            currentIndex++;
        }

        return reservoir;
    }
}
