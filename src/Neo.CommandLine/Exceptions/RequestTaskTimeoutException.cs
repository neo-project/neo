// Copyright (C) 2015-2024 The Neo Project.
//
// RequestTaskTimeoutException.cs file belongs to the neo project and is free
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
    internal class RequestTaskTimeoutException : Exception
    {
        public RequestTaskTimeoutException() : base("A task was canceled.")
        {
        }

        public RequestTaskTimeoutException(string? message) : base(message)
        {
        }

        public RequestTaskTimeoutException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RequestTaskTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
