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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Json
{
    public static class Extensions
    {
        extension(JsonNode node)
        {
            public double AsNumber()
            {
                return node.GetValueKind() switch
                {
                    JsonValueKind.Number => node.GetValue<double>(),
                    JsonValueKind.String => double.TryParse(node.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new InvalidCastException(),
                    _ => throw new InvalidCastException()
                };
            }

            public string AsString()
            {
                return node.GetValueKind() switch
                {
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Number => node.ToString(),
                    JsonValueKind.String => node.GetValue<string>(),
                    _ => throw new InvalidCastException()
                };
            }

            public T GetEnum<T>(bool ignoreCase = false) where T : unmanaged, Enum
            {
                var result = Enum.Parse<T>(node.AsString(), ignoreCase);
                if (!Enum.IsDefined(result)) throw new InvalidCastException();
                return result;
            }

            public byte[] ToByteArray(bool indented)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms, new JsonWriterOptions
                {
                    Indented = indented,
                    SkipValidation = true
                });
                node.WriteTo(writer);
                writer.Flush();
                return ms.ToArray();
            }

            public string StrictToString(bool indented)
            {
                return Utility.StrictUTF8.GetString(node.ToByteArray(indented));
            }

            public JsonArray JsonPath(string expr)
            {
                JsonNode?[] objects = [node];
                if (expr.Length == 0) return [.. objects];

                Queue<JPathToken> tokens = new(JPathToken.Parse(expr));
                var first = tokens.Dequeue();
                if (first.Type != JPathTokenType.Root)
                    throw new FormatException($"Unexpected token {first.Type}");

                JPathToken.ProcessJsonPath(ref objects, tokens);
                return [.. objects];
            }
        }

        extension(JsonNode)
        {
            public static JsonNode? StrictParse([StringSyntax(StringSyntaxAttribute.Json)] string json, int max_nest = 64)
            {
                var options = new JsonDocumentOptions
                {
                    AllowDuplicateProperties = false,
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = max_nest
                };
                return JsonNode.Parse(json, documentOptions: options);
            }

            public static JsonNode? StrictParse(ReadOnlySpan<byte> utf8Json, int max_nest = 64)
            {
                var options = new JsonDocumentOptions
                {
                    AllowDuplicateProperties = false,
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = max_nest
                };
                return JsonNode.Parse(utf8Json, documentOptions: options);
            }
        }
    }
}
