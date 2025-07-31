// Copyright (C) 2015-2025 The Neo Project.
//
// ContractParameter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents a parameter of a contract method.
    /// </summary>
    public class ContractParameter
    {
        private static readonly Regex Base64Regex = new Regex(@"^[A-Za-z0-9+/]*={0,2}$", RegexOptions.Compiled);

        /// <summary>
        /// Helper method to convert string data to byte array.
        /// Supports Base64, Hex (with or without 0x prefix), and UTF-8 encoded strings.
        /// </summary>
        private static byte[] ParseDataString(string data)
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

            // Try Base64 first (most common case for backward compatibility)
            if (Base64Regex.IsMatch(data) && data.Length % 4 == 0)
            {
                try
                {
                    return Convert.FromBase64String(data);
                }
                catch
                {
                    // Not valid Base64, continue to other formats
                }
            }

            // Try Hex with 0x prefix
            if (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return data[2..].HexToBytes();
                }
                catch
                {
                    // Not valid hex, continue
                }
            }

            // Try Hex without prefix (must be even length and all hex chars)
            if (data.Length % 2 == 0 && Regex.IsMatch(data, @"^[0-9A-Fa-f]+$"))
            {
                try
                {
                    return data.HexToBytes();
                }
                catch
                {
                    // Not valid hex, continue
                }
            }

            // Fall back to UTF-8 encoding for Unicode strings
            return System.Text.Encoding.UTF8.GetBytes(data);
        }
        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public ContractParameterType Type;

        /// <summary>
        /// The value of the parameter.
        /// </summary>
        public object Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractParameter"/> class.
        /// </summary>
        public ContractParameter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractParameter"/> class with the specified type.
        /// </summary>
        /// <param name="type">The type of the parameter.</param>
        public ContractParameter(ContractParameterType type)
        {
            Type = type;
            Value = type switch
            {
                ContractParameterType.Any => null,
                ContractParameterType.Signature => new byte[64],
                ContractParameterType.Boolean => false,
                ContractParameterType.Integer => 0,
                ContractParameterType.Hash160 => new UInt160(),
                ContractParameterType.Hash256 => new UInt256(),
                ContractParameterType.ByteArray => Array.Empty<byte>(),
                ContractParameterType.PublicKey => ECCurve.Secp256r1.G,
                ContractParameterType.String => "",
                ContractParameterType.Array => new List<ContractParameter>(),
                ContractParameterType.Map => new List<KeyValuePair<ContractParameter, ContractParameter>>(),
                _ => throw new ArgumentException($"Unsupported parameter type: {type}", nameof(type)),
            };
        }

        /// <summary>
        /// Converts the parameter from a JSON object.
        /// </summary>
        /// <param name="json">The parameter represented by a JSON object.</param>
        /// <returns>The converted parameter.</returns>
        public static ContractParameter FromJson(JObject json)
        {
            ContractParameter parameter = new()
            {
                Type = Enum.Parse<ContractParameterType>(json["type"].GetString())
            };
            if (json["value"] != null)
                parameter.Value = parameter.Type switch
                {
                    ContractParameterType.Signature or ContractParameterType.ByteArray => ParseDataString(json["value"].AsString()),
                    ContractParameterType.Boolean => json["value"].AsBoolean(),
                    ContractParameterType.Integer => BigInteger.Parse(json["value"].AsString()),
                    ContractParameterType.Hash160 => UInt160.Parse(json["value"].AsString()),
                    ContractParameterType.Hash256 => UInt256.Parse(json["value"].AsString()),
                    ContractParameterType.PublicKey => ECPoint.Parse(json["value"].AsString(), ECCurve.Secp256r1),
                    ContractParameterType.String => json["value"].AsString(),
                    ContractParameterType.Array => ((JArray)json["value"]).Select(p => FromJson((JObject)p)).ToList(),
                    ContractParameterType.Map => ((JArray)json["value"]).Select(p => new KeyValuePair<ContractParameter, ContractParameter>(FromJson((JObject)p["key"]), FromJson((JObject)p["value"]))).ToList(),
                    _ => throw new ArgumentException($"Unsupported parameter type: {parameter.Type}", nameof(json)),
                };
            return parameter;
        }

        /// <summary>
        /// Sets the value of the parameter.
        /// </summary>
        /// <param name="text">The <see cref="string"/> form of the value.</param>
        public void SetValue(string text)
        {
            switch (Type)
            {
                case ContractParameterType.Signature:
                    byte[] signature = ParseDataString(text);
                    if (signature.Length != 64) throw new FormatException();
                    Value = signature;
                    break;
                case ContractParameterType.Boolean:
                    Value = string.Equals(text, bool.TrueString, StringComparison.OrdinalIgnoreCase);
                    break;
                case ContractParameterType.Integer:
                    Value = BigInteger.Parse(text);
                    break;
                case ContractParameterType.Hash160:
                    Value = UInt160.Parse(text);
                    break;
                case ContractParameterType.Hash256:
                    Value = UInt256.Parse(text);
                    break;
                case ContractParameterType.ByteArray:
                    Value = ParseDataString(text);
                    break;
                case ContractParameterType.PublicKey:
                    Value = ECPoint.Parse(text, ECCurve.Secp256r1);
                    break;
                case ContractParameterType.String:
                    Value = text;
                    break;
                default:
                    throw new ArgumentException($"The ContractParameterType '{Type}' is not supported.");
            }
        }

        /// <summary>
        /// Converts the parameter to a JSON object.
        /// </summary>
        /// <returns>The parameter represented by a JSON object.</returns>
        public JObject ToJson()
        {
            return ToJson(this, null);
        }

        private static JObject ToJson(ContractParameter parameter, HashSet<ContractParameter> context)
        {
            JObject json = new();
            json["type"] = parameter.Type;
            if (parameter.Value != null)
                switch (parameter.Type)
                {
                    case ContractParameterType.Signature:
                    case ContractParameterType.ByteArray:
                        json["value"] = Convert.ToBase64String((byte[])parameter.Value);
                        break;
                    case ContractParameterType.Boolean:
                        json["value"] = (bool)parameter.Value;
                        break;
                    case ContractParameterType.Integer:
                    case ContractParameterType.Hash160:
                    case ContractParameterType.Hash256:
                    case ContractParameterType.PublicKey:
                    case ContractParameterType.String:
                        json["value"] = parameter.Value.ToString();
                        break;
                    case ContractParameterType.Array:
                        context ??= [];
                        if (!context.Add(parameter)) throw new InvalidOperationException("Circular reference.");
                        json["value"] = new JArray(((IList<ContractParameter>)parameter.Value).Select(p => ToJson(p, context)));
                        if (!context.Remove(parameter)) throw new InvalidOperationException("Circular reference.");
                        break;
                    case ContractParameterType.Map:
                        context ??= [];
                        if (!context.Add(parameter)) throw new InvalidOperationException("Circular reference.");
                        json["value"] = new JArray(((IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value).Select(p =>
                        {
                            JObject item = new();
                            item["key"] = ToJson(p.Key, context);
                            item["value"] = ToJson(p.Value, context);
                            return item;
                        }));
                        if (!context.Remove(parameter)) throw new InvalidOperationException("Circular reference.");
                        break;
                }
            return json;
        }

        public override string ToString()
        {
            return ToString(this, null);
        }

        private static string ToString(ContractParameter parameter, HashSet<ContractParameter> context)
        {
            switch (parameter.Value)
            {
                case null:
                    return "(null)";
                case byte[] data:
                    return data.ToHexString();
                case IList<ContractParameter> data:
                    if (context is null) context = new HashSet<ContractParameter>();
                    if (context.Contains(parameter))
                    {
                        return "(array)";
                    }
                    else
                    {
                        context.Add(parameter);
                        StringBuilder sb = new();
                        sb.Append('[');
                        foreach (ContractParameter item in data)
                        {
                            sb.Append(ToString(item, context));
                            sb.Append(", ");
                        }
                        if (data.Count > 0)
                            sb.Length -= 2;
                        sb.Append(']');
                        return sb.ToString();
                    }
                case IList<KeyValuePair<ContractParameter, ContractParameter>> data:
                    if (context is null) context = new HashSet<ContractParameter>();
                    if (context.Contains(parameter))
                    {
                        return "(map)";
                    }
                    else
                    {
                        context.Add(parameter);
                        StringBuilder sb = new();
                        sb.Append('[');
                        foreach (var item in data)
                        {
                            sb.Append('{');
                            sb.Append(ToString(item.Key, context));
                            sb.Append(',');
                            sb.Append(ToString(item.Value, context));
                            sb.Append('}');
                            sb.Append(", ");
                        }
                        if (data.Count > 0)
                            sb.Length -= 2;
                        sb.Append(']');
                        return sb.ToString();
                    }
                default:
                    return parameter.Value.ToString();
            }
        }
    }
}
