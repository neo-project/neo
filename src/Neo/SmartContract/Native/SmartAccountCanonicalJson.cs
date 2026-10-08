// Copyright (C) 2015-2026 The Neo Project.
//
// SmartAccountCanonicalJson.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract.Manifest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neo.SmartContract.Native
{
    /// <summary>RFC 8785 manifest encoding, separate from legacy Neo JSON serialization.</summary>
    internal static class SmartAccountCanonicalJson
    {
        private const int MaximumDepth = 64;
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static byte[] Serialize(JToken? value)
        {
            StringBuilder output = new();
            Write(value, output, new HashSet<JToken>(ReferenceEqualityComparer.Instance), 0);
            if (StrictUtf8.GetByteCount(output.ToString()) > ContractManifest.MaxLength)
                throw new FormatException("The canonical manifest exceeds the manifest size limit.");
            return StrictUtf8.GetBytes(output.ToString());
        }

        private static void CheckLength(StringBuilder output)
        {
            if (output.Length > ContractManifest.MaxLength)
                throw new FormatException("The canonical manifest exceeds the manifest size limit.");
        }

        private static void Write(JToken? value, StringBuilder output, HashSet<JToken> ancestors, int depth)
        {
            switch (value)
            {
                case null: output.Append("null"); break;
                case JBoolean boolean: output.Append(boolean.Value ? "true" : "false"); break;
                case JNumber number: output.Append(FormatNumber(number.Value)); break;
                case JString text: WriteString(text.GetString(), output); break;
                case JContainer container:
                    if (depth >= MaximumDepth || !ancestors.Add(container))
                        throw new FormatException("The canonical manifest contains a cycle or exceeds the JSON nesting limit.");
                    try
                    {
                        bool first = true;
                        if (container is JObject obj)
                        {
                            output.Append('{');
                            foreach (var pair in obj.Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
                            {
                                if (!first) output.Append(',');
                                first = false;
                                WriteString(pair.Key, output);
                                output.Append(':');
                                Write(pair.Value, output, ancestors, depth + 1);
                            }
                            output.Append('}');
                        }
                        else
                        {
                            output.Append('[');
                            foreach (var item in container.Children)
                            {
                                if (!first) output.Append(',');
                                first = false;
                                Write(item, output, ancestors, depth + 1);
                            }
                            output.Append(']');
                        }
                    }
                    finally { ancestors.Remove(container); }
                    break;
                default: throw new FormatException("Unsupported canonical JSON value.");
            }
            CheckLength(output);
        }

        private static void WriteString(string value, StringBuilder output)
        {
            try { _ = StrictUtf8.GetByteCount(value); }
            catch (EncoderFallbackException error) { throw new FormatException("Canonical JSON rejects invalid Unicode.", error); }
            output.Append('"');
            foreach (char character in value)
            {
                output.Append(character switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\b' => "\\b",
                    '\t' => "\\t",
                    '\n' => "\\n",
                    '\f' => "\\f",
                    '\r' => "\\r",
                    < ' ' => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                    _ => character.ToString()
                });
                CheckLength(output);
            }
            output.Append('"');
        }

        internal static string FormatNumber(double value)
        {
            if (!double.IsFinite(value)) throw new FormatException("Canonical JSON requires finite numbers.");
            if (value == 0) return "0";
            // .NET's shortest round-trip digits use nearest/even ties. Apply the
            // ECMAScript decimal/exponent presentation thresholds to those digits.
            string raw = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture);
            int exponentIndex = raw.IndexOf('E');
            int exponent = exponentIndex < 0 ? 0 : int.Parse(raw[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
            string mantissa = exponentIndex < 0 ? raw : raw[..exponentIndex];
            int point = mantissa.IndexOf('.');
            int position = (point < 0 ? mantissa.Length : point) + exponent;
            string digits = mantissa.Replace(".", "", StringComparison.Ordinal);
            int leading = 0;
            while (digits[leading] == '0') leading++;
            position -= leading;
            digits = digits[leading..].TrimEnd('0');
            string number;
            if (position > 0 && position <= 21)
                number = digits.Length <= position ? digits + new string('0', position - digits.Length) : digits.Insert(position, ".");
            else if (position <= 0 && position > -6)
                number = "0." + new string('0', -position) + digits;
            else
                number = digits[0] + (digits.Length == 1 ? "" : "." + digits[1..]) + "e" +
                    (position > 0 ? "+" : "") + (position - 1).ToString(CultureInfo.InvariantCulture);
            return value < 0 ? "-" + number : number;
        }
    }
}
