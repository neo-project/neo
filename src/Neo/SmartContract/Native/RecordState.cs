// Copyright (C) 2015-2026 The Neo Project.
//
// RecordState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// DNS-like record payload for native NameService.
    /// </summary>
    public class RecordState : IInteroperable
    {
        public string Name = string.Empty;
        public RecordType Type;
        public string Data = string.Empty;

        public void FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Name = @struct[0].GetString() ?? string.Empty;
            Type = (RecordType)(byte)@struct[1].GetInteger();
            Data = @struct[2].GetString() ?? string.Empty;
        }

        public StackItem ToStackItem()
        {
            return new Struct()
            {
                Name,
                (byte)Type,
                Data
            };
        }
    }
}
