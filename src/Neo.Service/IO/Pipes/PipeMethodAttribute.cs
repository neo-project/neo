// Copyright (C) 2015-2024 The Neo Project.
//
// PipeMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Service.IO.Pipes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class PipeMethodAttribute : Attribute, IEquatable<PipeMethodAttribute>
    {
        public PipeCommand Command { get; }
        public bool Overwrite { get; set; } = false;
        public bool Awaited { get; } = false;

        public PipeMethodAttribute(
            PipeCommand command,
            bool shouldAwait)
        {
            Command = command;
            Awaited = shouldAwait;
        }

        public bool Equals(PipeMethodAttribute? other)
        {
            if (other is null) return false;
            return Command == other.Command;
        }

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other as PipeMethodAttribute);
        }

        public override int GetHashCode() =>
            HashCode.Combine(Command, Awaited, Overwrite);
    }
}
