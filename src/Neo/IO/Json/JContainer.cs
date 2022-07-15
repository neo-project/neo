using System.Collections.Generic;

namespace Neo.IO.Json;

public abstract class JContainer : JToken
{
    /// <summary>
    /// Gets the token at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the token to get.</param>
    /// <returns>The token at the specified index.</returns>
    public virtual JToken this[int index] => Children[index];

    public abstract IReadOnlyList<JToken> Children { get; }

    public int Count => Children.Count;

    public abstract void Clear();

    public void CopyTo(JToken[] array, int arrayIndex)
    {
        for (int i = 0; i < Count && i + arrayIndex < array.Length; i++)
            array[i + arrayIndex] = Children[i];
    }
}
