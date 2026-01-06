// Copyright (C) 2015-2026 The Neo Project.
//
// KeyPath.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo.Wallets.BIP32
{

    internal partial class KeyPath
    {
        [GeneratedRegex(@"^\s*m(?:\s*/\s*(?<index>\d+)\s*(?<hardened>'?)\s*)*\s*$")]
        private static partial Regex KeyPathRegex();

        public static KeyPath Master { get; } = new(Array.Empty<uint>());
        public ImmutableArray<uint> Indices { get; }

        KeyPath(IReadOnlyList<uint> indices)
        {
            Indices = indices.ToImmutableArray();
        }

        public KeyPath Derive(uint index)
        {
            uint[] newIndices = new uint[Indices.Length + 1];
            Indices.CopyTo(newIndices, 0);
            newIndices[Indices.Length] = index;
            return new KeyPath(newIndices);
        }

        public static KeyPath Parse(string path)
        {
            Match match = KeyPathRegex().Match(path);
            if (!match.Success) throw new FormatException();
            int count = match.Groups["index"].Captures.Count;
            uint[] indices = new uint[count];
            for (int i = 0; i < count; i++)
            {
                indices[i] = uint.Parse(match.Groups["index"].Captures[i].Value);
                if (indices[i] >= 0x80000000) throw new FormatException();
                bool hardened = match.Groups["hardened"].Captures[i].Length > 0;
                if (hardened) indices[i] |= 0x80000000;
            }
            return new KeyPath(indices);
        }

        public override string ToString()
        {
            StringBuilder builder = new("m");
            foreach (uint index in Indices)
            {
                builder.Append('/');
                if ((index & 0x80000000) != 0)
                    builder.Append(index & ~0x80000000).Append('\'');
                else
                    builder.Append(index);
            }
            return builder.ToString();
        }
    }
}
