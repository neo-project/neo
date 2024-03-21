// Copyright (C) 2015-2024 The Neo Project.
//
// PipeMessageException.cs file belongs to the neo project and is free
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
    internal sealed class PipeMessageException : Exception
    {
        public PipeMessageException()
        {
        }

        public PipeMessageException(string? message) : base(message)
        {
        }

        public PipeMessageException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected PipeMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
