// Copyright (C) 2015-2024 The Neo Project.
//
// KeyValuePairsDebugView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.Plugins.Storage
{
    internal class KeyValuePairsDebugView(
            IEnumerable<KeyValuePair<byte[], byte[]>> data)
    {
        private readonly IEnumerable<KeyValuePair<byte[], byte[]>> _data = data;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePairs[] Keys
        {
            get
            {
                var keys = new List<KeyValuePairs>();

                foreach (var item in _data)
                    keys.Add(new KeyValuePairs(item.Key, item.Value));
                return [.. keys];
            }
        }
    }
}
