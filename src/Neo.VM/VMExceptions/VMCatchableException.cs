// Copyright (C) 2015-2024 The Neo Project.
//
// VMCatchableException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM.Exceptions
{
    public class VMCatchableException : Exception, IVMException
    {
        public VMCatchableException(string message) : base($"{message}")
        {
<<<<<<<< HEAD:src/Neo.VM/VMExceptions/VMCatchableException.cs
========
            if (string.IsNullOrEmpty(message))
                throw new VMUncatchableException("Message cannot be null or empty.");
>>>>>>>> 3db7457a55dc584dd29e5d9cff882d560ba46db0:src/Neo.VM/Exceptions/VMCatchableException.cs
        }
    }
}
