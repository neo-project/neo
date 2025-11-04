// Copyright (C) 2015-2025 The Neo Project.
//
// Extensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Json
{
    public static class Extensions
    {
        public static string AsString(this JsonNode json)
        {
            return json.GetValueKind() switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => json.ToString(),
                JsonValueKind.String => json.GetValue<string>(),
                _ => throw new InvalidCastException()
            };
        }

        public static T GetEnum<T>(this JsonNode json, bool ignoreCase = false) where T : unmanaged, Enum
        {
            switch (json.GetValueKind())
            {
                case JsonValueKind.Number:
                    {
                        var enumType = typeof(T);
                        object value;
                        try
                        {
                            value = Convert.ChangeType(json.GetValue<double>(), enumType.GetEnumUnderlyingType());
                        }
                        catch (OverflowException)
                        {
                            throw new InvalidCastException($"The value is out of range for the enum {enumType.FullName}");
                        }
                        var result = Enum.ToObject(enumType, value);
                        if (!Enum.IsDefined(enumType, result))
                            throw new InvalidCastException($"The value is not defined in the enum {enumType.FullName}");
                        return (T)result;
                    }
                case JsonValueKind.String:
                    {
                        var result = Enum.Parse<T>(json.GetValue<string>(), ignoreCase);
                        if (!Enum.IsDefined(result)) throw new InvalidCastException();
                        return result;
                    }
                default:
                    throw new InvalidCastException();
            }
        }

        public static byte[] ToByteArray(this JsonNode json, bool indented)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms, new JsonWriterOptions
            {
                Indented = indented,
                SkipValidation = true
            });
            json.WriteTo(writer);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
