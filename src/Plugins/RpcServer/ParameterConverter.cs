// Copyright (C) 2015-2025 The Neo Project.
//
// ParameterConverter.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RpcServer.Model;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using JToken = Neo.Json.JToken;

namespace Neo.Plugins.RpcServer
{
    public static class ParameterConverter
    {
        private static readonly Dictionary<Type, Func<JToken, object>> s_conversionStrategies;

        static ParameterConverter()
        {
            s_conversionStrategies = new Dictionary<Type, Func<JToken, object>>
            {
                { typeof(string), token => Result.Ok_Or(token.AsString, CreateInvalidParamError<string>(token)) },
                { typeof(byte), ConvertNumeric<byte> },
                { typeof(sbyte), ConvertNumeric<sbyte> },
                { typeof(short), ConvertNumeric<short> },
                { typeof(ushort), ConvertNumeric<ushort> },
                { typeof(int), ConvertNumeric<int> },
                { typeof(uint), ConvertNumeric<uint> },
                { typeof(long), ConvertNumeric<long> },
                { typeof(ulong), ConvertNumeric<ulong> },
                { typeof(double), token => Result.Ok_Or(token.AsNumber, CreateInvalidParamError<double>(token)) },
                { typeof(bool), token => Result.Ok_Or(token.AsBoolean, CreateInvalidParamError<bool>(token)) },
                { typeof(UInt256), ConvertUInt256 },
                { typeof(ContractNameOrHashOrId), ConvertContractNameOrHashOrId },
                { typeof(BlockHashOrIndex), ConvertBlockHashOrIndex }
            };
        }

        internal static object ConvertParameter(JToken token, Type targetType)
        {
            if (s_conversionStrategies.TryGetValue(targetType, out var conversionStrategy))
                return conversionStrategy(token);
            throw new RpcException(RpcError.InvalidParams.WithData($"Unsupported parameter type: {targetType}"));
        }

        private static object ConvertNumeric<T>(JToken token) where T : struct
        {
            if (TryConvertDoubleToNumericType<T>(token, out var result))
            {
                return result;
            }

            throw new RpcException(CreateInvalidParamError<T>(token));
        }

        private static bool TryConvertDoubleToNumericType<T>(JToken token, out T result) where T : struct
        {
            result = default;
            try
            {
                var value = token.AsNumber();
                var minValue = Convert.ToDouble(typeof(T).GetField("MinValue").GetValue(null));
                var maxValue = Convert.ToDouble(typeof(T).GetField("MaxValue").GetValue(null));

                if (value < minValue || value > maxValue)
                {
                    return false;
                }

                if (!typeof(T).IsFloatingPoint() && !IsValidInteger(value))
                {
                    return false;
                }

                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidInteger(double value)
        {
            // Integer values are safe if they are within the range of MIN_SAFE_INTEGER and MAX_SAFE_INTEGER
            if (value < JNumber.MIN_SAFE_INTEGER || value > JNumber.MAX_SAFE_INTEGER)
                return false;
            return Math.Abs(value % 1) <= double.Epsilon;
        }

        internal static object ConvertUInt160(JToken token, byte addressVersion)
        {
            var value = token.AsString();
            if (UInt160.TryParse(value, out var scriptHash))
            {
                return scriptHash;
            }
            return Result.Ok_Or(() => value.ToScriptHash(addressVersion),
                RpcError.InvalidParams.WithData($"Invalid UInt160 Format: {token}"));
        }

        private static object ConvertUInt256(JToken token)
        {
            if (UInt256.TryParse(token.AsString(), out var hash))
            {
                return hash;
            }
            throw new RpcException(RpcError.InvalidParams.WithData($"Invalid UInt256 Format: {token}"));
        }

        private static object ConvertContractNameOrHashOrId(JToken token)
        {
            if (ContractNameOrHashOrId.TryParse(token.AsString(), out var contractNameOrHashOrId))
            {
                return contractNameOrHashOrId;
            }
            throw new RpcException(RpcError.InvalidParams.WithData($"Invalid contract hash or id Format: {token}"));
        }

        private static object ConvertBlockHashOrIndex(JToken token)
        {
            if (BlockHashOrIndex.TryParse(token.AsString(), out var blockHashOrIndex))
            {
                return blockHashOrIndex;
            }
            throw new RpcException(RpcError.InvalidParams.WithData($"Invalid block hash or index Format: {token}"));
        }

        private static RpcError CreateInvalidParamError<T>(JToken token)
        {
            return RpcError.InvalidParams.WithData($"Invalid {typeof(T)} value: {token}");
        }

        /// <summary>
        /// Create a SignersAndWitnesses from a JSON array.
        /// Each item in the JSON array should be a JSON object with the following properties:
        /// - "signer": A JSON object with the following properties:
        ///   - "account": A hex-encoded UInt160 or a Base58Check address, required.
        ///   - "scopes": A enum string representing the scopes(WitnessScope) of the signer, required.
        ///   - "allowedcontracts": An array of hex-encoded UInt160, optional.
        ///   - "allowedgroups": An array of hex-encoded ECPoint, optional.
        ///   - "rules": An array of strings representing the rules(WitnessRule) of the signer, optional.
        /// - "witness": A JSON object with the following properties:
        ///   - "invocation": A base64-encoded string representing the invocation script, optional.
        ///   - "verification": A base64-encoded string representing the verification script, optional.
        /// </summary>
        /// <param name="json">The JSON array to create a SignersAndWitnesses from.</param>
        /// <param name="addressVersion">The address version to use for the signers.</param>
        /// <returns>A SignersAndWitnesses object.</returns>
        /// <exception cref="RpcException">Thrown when the JSON array is invalid.</exception>
        internal static (Signer[] Signers, Witness[] Witnesses) ToSignersAndWitnesses(this JArray json, byte addressVersion)
        {
            var signers = json.ToSigners(addressVersion);
            var witnesses = json.ToWitnesses();
            return new(signers, witnesses);
        }

        /// <summary>
        /// Create a Signer array from a JSON array.
        /// Each item in the JSON array should be a JSON object with the following properties:
        /// - "account": A hex-encoded UInt160 or a Base58Check address, required.
        /// - "scopes": A enum string representing the scopes(WitnessScope) of the signer, required.
        /// - "allowedcontracts": An array of hex-encoded UInt160, optional.
        /// - "allowedgroups": An array of hex-encoded ECPoint, optional.
        /// - "rules": An array of strings representing the rules(WitnessRule) of the signer, optional.
        /// </summary>
        /// <param name="json">The JSON array to create a Signer array from.</param>
        /// <param name="addressVersion">The address version to use for the signers.</param>
        /// <returns>A Signer array.</returns>
        /// <exception cref="RpcException">Thrown when the JSON array is invalid or max allowed witness exceeded.</exception>
        internal static Signer[] ToSigners(this JArray json, byte addressVersion)
        {
            if (json.Count > Transaction.MaxTransactionAttributes)
                throw new RpcException(RpcError.InvalidParams.WithData("Max allowed witness exceeded."));

            var ret = json.Select(u => new Signer
            {
                Account = u["account"].AsString().AddressToScriptHash(addressVersion),
                Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), u["scopes"]?.AsString()),
                AllowedContracts = ((JArray)u["allowedcontracts"])?.Select(p => UInt160.Parse(p.AsString())).ToArray() ?? [],
                AllowedGroups = ((JArray)u["allowedgroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray() ?? [],
                Rules = ((JArray)u["rules"])?.Select(r => WitnessRule.FromJson((JObject)r)).ToArray() ?? [],
            }).ToArray();

            // Validate format
            _ = ret.ToByteArray().AsSerializableArray<Signer>();
            return ret;
        }

        /// <summary>
        /// Create a Witness array from a JSON array.   
        /// Each item in the JSON array should be a JSON object with the following properties:
        /// - "invocation": A base64-encoded string representing the invocation script, optional.
        /// - "verification": A base64-encoded string representing the verification script, optional.
        /// </summary>
        /// <param name="json">The JSON array to create a Witness array from.</param>
        /// <returns>A Witness array.</returns>
        /// <exception cref="RpcException">Thrown when the JSON array is invalid or max allowed witness exceeded.</exception>
        internal static Witness[] ToWitnesses(this JArray json)
        {
            if (json.Count > Transaction.MaxTransactionAttributes)
                throw new RpcException(RpcError.InvalidParams.WithData("Max allowed witness exceeded."));

            return json.Select(u => new
            {
                Invocation = u["invocation"]?.AsString(),
                Verification = u["verification"]?.AsString()
            })
            .Where(x => x.Invocation != null || x.Verification != null)
            .Select(x => new Witness()
            {
                InvocationScript = Convert.FromBase64String(x.Invocation ?? string.Empty),
                VerificationScript = Convert.FromBase64String(x.Verification ?? string.Empty)
            })
            .ToArray();
        }

        /// <summary>
        /// Converts an hex-encoded UInt160 or a Base58Check address to a script hash.
        /// </summary>
        /// <param name="address">The address to convert.</param>
        /// <param name="version">The address version to use for the conversion.</param>
        /// <returns>The script hash corresponding to the address.</returns>
        internal static UInt160 AddressToScriptHash(this string address, byte version)
        {
            if (UInt160.TryParse(address, out var scriptHash))
                return scriptHash;
            return address.ToScriptHash(version);
        }
    }

    public static class TypeExtensions
    {
        public static bool IsFloatingPoint(this Type type)
        {
            return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }
    }
}
