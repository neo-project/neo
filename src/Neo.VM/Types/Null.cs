// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents <see langword="null"/> in the VM.
    /// </summary>
    public class Null : StackItem
    {
        public override StackItemType Type => StackItemType.Any;

        internal Null() { }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidCastException($"Type can't be converted to StackItemType: {type}");
            return this;
        }

        public override bool Equals(StackItem? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Null;
        }

        public override bool GetBoolean()
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        [return: MaybeNull]
        public override T GetInterface<T>()
        {
            return default;
        }

        public override string? GetString()
        {
            return null;
        }
    }
}
