// Copyright (C) 2015-2024 The Neo Project.
//
// VMUncatchableException.cs file belongs to the neo project and is free
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
    /// <summary>
    /// Represents an exception that will be thrown during the execution of the VM,
    /// is not catchable by the VM, and will directly cause the VM execution to fault.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public class VmUncatchableException(string message) : Exception(message), IVMException
    {
    }

}
