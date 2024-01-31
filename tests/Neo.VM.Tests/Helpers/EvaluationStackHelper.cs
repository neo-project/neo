// Copyright (C) 2015-2024 The Neo Project.
//
// EvaluationStackHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.Test.Helpers;

public static class EvaluationStackHelper
{
    public static string Print(this EvaluationStack stack)
    {
        return $"[{string.Join(", ", stack.Select(p =>
            {
                var value = p.Type switch
                {
                    StackItemType.Pointer => $"({((Pointer)p).Position})",
                    StackItemType.Boolean => $"({p.GetBoolean()})",
                    StackItemType.Integer => $"({p.GetInteger()})",
                    // If the bytestring is not a valid UTF-8 string, we'll just print the base64 representation
                    StackItemType.ByteString => p.GetSpan().ToArray().TryGetString(out var str) ? $"(\"{str}\")" : $"(\"Base64: {Convert.ToBase64String(p.GetSpan())}\")",
                    StackItemType.Array
                        or StackItemType.Map
                        or StackItemType.Struct => $"({((CompoundType)p).Count})",
                    _ => ""
                };
                return $"{p.Type.ToString()}{value}";
            }
        ))}]";
    }
}
