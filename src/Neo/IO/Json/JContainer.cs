using System.Collections.Generic;

namespace Neo.IO.Json;

public abstract class JContainer : JToken
{
    public override JToken this[int index] => Children[index];

    public abstract IReadOnlyList<JToken> Children { get; }

    public int Count => Children.Count;

    public abstract void Clear();

    public void CopyTo(JToken[] array, int arrayIndex)
    {
        for (int i = 0; i < Count && i + arrayIndex < array.Length; i++)
            array[i + arrayIndex] = Children[i];
    }
}
