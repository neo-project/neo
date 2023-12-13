// Copyright (C) 2015-2022 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json;

public abstract class JContainer : JToken
{
    public override JToken? this[int index] => Children[index];

    public abstract IReadOnlyList<JToken?> Children { get; }

    public int Count => Children.Count;

    public abstract void Clear();

    public void CopyTo(JToken?[] array, int arrayIndex)
    {
        for (int i = 0; i < Count && i + arrayIndex < array.Length; i++)
            array[i + arrayIndex] = Children[i];
    }
}
