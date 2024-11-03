// Copyright (C) 2015-2024 The Neo Project.
//
// NewByteArrayCompare.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.Extensions;

public class NewByteArrayCompare : IComparer<byte[]>
{
    public static readonly NewByteArrayCompare Default = new(1);
    public static readonly NewByteArrayCompare Reverse = new(-1);

    private readonly int _direction;

    private NewByteArrayCompare(int direction)
    {
        _direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(byte[]? x, byte[]? y)
    {
        if (_direction < 0)
            return y.AsSpan().SequenceCompareTo(x.AsSpan());
        return x.AsSpan().SequenceCompareTo(y.AsSpan());
    }
}
