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

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Json
{
    public static class Extensions
    {
        public static double AsNumber(this JsonNode json)
        {
            return json.GetValueKind() switch
            {
                JsonValueKind.Number => json.GetValue<double>(),
                JsonValueKind.String => double.TryParse(json.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new InvalidCastException(),
                _ => throw new InvalidCastException()
            };
        }

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
            var result = Enum.Parse<T>(json.AsString(), ignoreCase);
            if (!Enum.IsDefined(result)) throw new InvalidCastException();
            return result;
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

        public static string StrictToString(this JsonNode json, bool indented)
        {
            return Utility.StrictUTF8.GetString(json.ToByteArray(indented));
        }

        public static JsonArray JsonPath(this JsonNode json, string expr)
        {
            JsonNode?[] objects = [json];
            if (expr.Length == 0) return [.. objects];

            Queue<JPathToken> tokens = new(JPathToken.Parse(expr));
            var first = tokens.Dequeue();
            if (first.Type != JPathTokenType.Root)
                throw new FormatException($"Unexpected token {first.Type}");

            JPathToken.ProcessJsonPath(ref objects, tokens);
            return [.. objects];
        }
    }
}
