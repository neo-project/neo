// Copyright (C) 2015-2024 The Neo Project.
//
// PipeException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Buffers;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeException : IPipeMessage
    {
        public string Message { get; set; }

        public string StackTrace { get; set; }

        public PipeException()
        {
            Message = string.Empty;
            StackTrace = string.Empty;
        }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Message) &&
            string.IsNullOrEmpty(StackTrace);

        public int Size =>
            Struffer.SizeOf(Message) +
            Struffer.SizeOf(StackTrace);

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Struffer(buffer);

            Message = wrapper.ReadString();
            StackTrace = wrapper.ReadString();
        }

        public byte[] ToArray()
        {
            var wrapper = new Struffer(Size);

            wrapper.Write(Message);
            wrapper.Write(StackTrace);

            return [.. wrapper];
        }
    }
}
