// Copyright (C) 2015-2024 The Neo Project.
//
// ExceptionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.CommandLine.Services.Payloads
{
    internal sealed class ExceptionPayload : ISerializable
    {
        public int Code { get; private set; }
        public string? Message { get; private set; }
#if DEBUG
        public string? StackTrace { get; private set; }
#endif

        public static ExceptionPayload Create(Exception exception) =>
            new()
            {
                Code = exception.HResult,
                Message = exception.Message,
#if DEBUG
                StackTrace = exception.StackTrace,
#endif
            };

        #region ISerializable

        int ISerializable.Size =>
            sizeof(int) +           // Code
            Message.GetVarSize();   // Message

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Code = reader.ReadInt32();
            Message = reader.ReadVarString();
#if DEBUG
            StackTrace = reader.ReadVarString();
#endif
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(Code);
            writer.WriteVarString(Message);
#if DEBUG
            writer.WriteVarString(StackTrace);
#endif
        }

        #endregion
    }
}
