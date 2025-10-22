// Copyright (C) 2015-2025 The Neo Project.
//
// DiscoveryResponseMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.Messages
{
    internal class DiscoveryResponseMessage
    {
        private readonly IDictionary<string, string> _headers;

        public DiscoveryResponseMessage(string message)
        {
            var lines = message.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            var headers = from h in lines.Skip(1)
                          let c = h.Split(':')
                          let key = c[0]
                          let value = c.Length > 1
                              ? string.Join(":", c.Skip(1).ToArray())
                              : string.Empty
                          select new { Key = key, Value = value.Trim() };
            _headers = headers.ToDictionary(x => x.Key.ToUpperInvariant(), x => x.Value);
        }

        public string this[string key]
        {
            get { return _headers[key.ToUpperInvariant()]; }
        }
    }
}
