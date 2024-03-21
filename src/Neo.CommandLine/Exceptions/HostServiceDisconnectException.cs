// Copyright (C) 2015-2024 The Neo Project.
//
// HostServiceDisconnectException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.Serialization;

namespace Neo.CommandLine.Exceptions
{
    internal class HostServiceDisconnectException : Exception
    {
        public HostServiceDisconnectException() : base("Connection closed.")
        {
        }

        public HostServiceDisconnectException(string? message) : base(message)
        {
        }

        public HostServiceDisconnectException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected HostServiceDisconnectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
