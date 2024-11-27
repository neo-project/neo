// Copyright (C) 2015-2024 The Neo Project.
//
// VMUnhandledException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.VM.Exceptions
{
    /// <summary>
    /// Represents an unhandled exception in the VM.
    /// Thrown when there is an exception in the VM that is not caught by any script.
    /// </summary>
<<<<<<<< HEAD:src/Neo.VM/VMExceptions/VMUnhandledException.cs
    public class VMUnhandledException : VMCatchableException
========
    public class VMUnhandledException : Exception, IVMException
>>>>>>>> 3db7457a55dc584dd29e5d9cff882d560ba46db0:src/Neo.VM/Exceptions/VMUnhandledException.cs
    {
        /// <summary>
        /// The unhandled exception in the VM.
        /// </summary>
        public StackItem ExceptionObject { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VMUnhandledException"/> class.
        /// </summary>
        /// <param name="ex">The unhandled exception in the VM.</param>
        public VMUnhandledException(StackItem ex) : base(GetExceptionMessage(ex))
        {
            ExceptionObject = ex;
        }

        private static string GetExceptionMessage(StackItem e)
        {
            StringBuilder sb = new("An unhandled exception was thrown.");
            var s = e as ByteString;
            if (s is null && e is Array array && array.Count > 0)
                s = array[0] as ByteString;
            if (s != null)
            {
                sb.Append(' ');
                sb.Append(Encoding.UTF8.GetString(s.GetSpan()));
            }
            return sb.ToString();
        }
    }
}
