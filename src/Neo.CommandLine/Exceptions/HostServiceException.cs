// Copyright (C) 2015-2024 The Neo Project.
//
// HostServiceException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CommandLine.Services.Payloads;
using System;
using System.Runtime.Serialization;

namespace Neo.CommandLine.Exceptions
{
    internal sealed class HostServiceException : Exception
    {
        public HostServiceException()
        {
        }

#if DEBUG
        public HostServiceException(ExceptionPayload payload) : base($"{payload.Code}: \"{payload.Message}\"{Environment.NewLine}{payload.StackTrace}")
        {
        }
#else
        public HostServiceException(ExceptionPayload payload) : base($"{payload.Code}: \"{payload.Message}\"")
        {
        }
#endif

        public HostServiceException(string? message) : base(message)
        {
        }

        public HostServiceException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected HostServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
