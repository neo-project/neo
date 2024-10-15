// Copyright (C) 2015-2024 The Neo Project.
//
// PipeExceptionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Buffers;
using System;

namespace Neo.Plugins.Models.Payloads
{
    internal sealed class PipeExceptionPayload : IPipeMessage
    {
        public string Type { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public PipeExceptionPayload()
        {
            Type = nameof(PipeExceptionPayload);
            Message = string.Empty;
            StackTrace = string.Empty;
        }

        public PipeExceptionPayload(
            Exception exception)
        {
            Type = exception.GetType().Name;
            Message = exception.Message;
            StackTrace = exception.StackTrace ?? string.Empty;
        }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Message) &&
            string.IsNullOrEmpty(StackTrace);
        public int Size =>
            Stuffer.SizeOf(Type) +
            Stuffer.SizeOf(Message) +
            Stuffer.SizeOf(StackTrace);

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            Type = wrapper.ReadString();
            Message = wrapper.ReadString();
            StackTrace = wrapper.ReadString();
        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write(Type);
            wrapper.Write(Message);
            wrapper.Write(StackTrace);

            return [.. wrapper];
        }
    }
}
