// Copyright (C) 2015-2025 The Neo Project.
//
// NEP11TokenModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.Plugins.RestServer.Models.Token
{
    internal class NEP11TokenModel : NEP17TokenModel
    {
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, StackItem>?> Tokens { get; set; }
            = new Dictionary<string, IReadOnlyDictionary<string, StackItem>?>().AsReadOnly();
    }
}
