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

using Neo.CLI.Pipes.Buffers;
using System;

namespace Neo.CLI.Pipes.Protocols.Payloads
{
    internal class ExceptionPayload : INamedPipeMessage
    {
        public int HResult { get; set; }
        public string ExceptionName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;

        public int Size =>
            sizeof(int) +                                // HResult
            MemoryBuffer.GetStringSize(ExceptionName) + // ExceptionType
            MemoryBuffer.GetStringSize(Message) +        // Message
            MemoryBuffer.GetStringSize(StackTrace);     // StackTrace

        public static ExceptionPayload FromException(Exception ex) =>
            new()
            {
                HResult = ex.HResult,
                ExceptionName = ex.GetType().FullName ?? string.Empty,
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty
            };

        public void FromBytes(byte[] buffer)
        {
            using var reader = new MemoryBuffer(buffer);
            FromMemoryBuffer(reader);
        }

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            HResult = reader.Read<int>();

            var str = reader.ReadString();
            if (string.IsNullOrEmpty(str) == false)
                ExceptionName = str;

            str = reader.ReadString();
            if (string.IsNullOrEmpty(str) == false)
                Message = str;

            str = reader.ReadString();
            if (string.IsNullOrEmpty(str) == false)
                StackTrace = str;
        }

        public byte[] ToByteArray()
        {
            using var writer = new MemoryBuffer();
            writer.Write(HResult);

            if (string.IsNullOrEmpty(ExceptionName) == false)
                writer.WriteString(ExceptionName);

            if (string.IsNullOrEmpty(Message) == false)
                writer.WriteString(Message);

            if (string.IsNullOrEmpty(StackTrace) == false)
                writer.WriteString(StackTrace);

            return writer.ToArray();
        }
    }
}
