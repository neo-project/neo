// Copyright (C) 2015-2025 The Neo Project.
//
// RestServerUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RestServer.Models.Contract;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Plugins.RestServer
{
    public static partial class RestServerUtility
    {
        private readonly static Script s_emptyScript = System.Array.Empty<byte>();

        public static UInt160 ConvertToScriptHash(string address, ProtocolSettings settings)
        {
            if (UInt160.TryParse(address, out var scriptHash))
                return scriptHash;
            return address.ToScriptHash(settings.AddressVersion);
        }

        public static bool TryConvertToScriptHash(string address, ProtocolSettings settings, out UInt160 scriptHash)
        {
            try
            {
                if (UInt160.TryParse(address, out scriptHash))
                    return true;
                scriptHash = address.ToScriptHash(settings.AddressVersion);
                return true;
            }
            catch
            {
                scriptHash = UInt160.Zero;
                return false;
            }
        }

        public static StackItem StackItemFromJToken(JToken? json)
        {
            if (json is null) return StackItem.Null;

            if (json.Type == JTokenType.Object && json is JObject jsonObject)
            {
                var props = jsonObject.Properties();
                var typeProp = props.SingleOrDefault(s => s.Name.Equals("type", StringComparison.InvariantCultureIgnoreCase));
                var valueProp = props.SingleOrDefault(s => s.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase));

                if (typeProp != null && valueProp != null)
                {
                    StackItem s = StackItem.Null;
                    var type = Enum.Parse<StackItemType>(typeProp.ToObject<string>() ?? throw new ArgumentNullException(), true);
                    var value = valueProp.Value;

                    switch (type)
                    {
                        case StackItemType.Struct:
                            if (value.Type == JTokenType.Array)
                            {
                                var st = new Struct();
                                foreach (var item in (JArray)value)
                                    st.Add(StackItemFromJToken(item));
                                s = st;
                            }
                            break;
                        case StackItemType.Array:
                            if (value.Type == JTokenType.Array)
                            {
                                var a = new Array();
                                foreach (var item in (JArray)value)
                                    a.Add(StackItemFromJToken(item));
                                s = a;
                            }
                            break;
                        case StackItemType.Map:
                            if (value.Type == JTokenType.Array)
                            {
                                var m = new Map();
                                foreach (var item in (JArray)value)
                                {
                                    if (item.Type != JTokenType.Object)
                                        continue;
                                    var vprops = ((JObject)item).Properties();
                                    var keyProps = vprops.SingleOrDefault(s => s.Name.Equals("key", StringComparison.InvariantCultureIgnoreCase));
                                    var keyValueProps = vprops.SingleOrDefault(s => s.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase));
                                    if (keyProps == null && keyValueProps == null)
                                        continue;
                                    var key = (PrimitiveType)StackItemFromJToken(keyProps?.Value);
                                    m[key] = StackItemFromJToken(keyValueProps?.Value);
                                }
                                s = m;
                            }
                            break;
                        case StackItemType.Boolean:
                            s = value.ToObject<bool>() ? StackItem.True : StackItem.False;
                            break;
                        case StackItemType.Buffer:
                            s = new Buffer(Convert.FromBase64String(value.ToObject<string>() ?? throw new ArgumentNullException()));
                            break;
                        case StackItemType.ByteString:
                            s = new ByteString(Convert.FromBase64String(value.ToObject<string>() ?? throw new ArgumentNullException()));
                            break;
                        case StackItemType.Integer:
                            s = value.ToObject<BigInteger>();
                            break;
                        case StackItemType.InteropInterface:
                            s = new InteropInterface(Convert.FromBase64String(value.ToObject<string>() ?? throw new ArgumentNullException()));
                            break;
                        case StackItemType.Pointer:
                            s = new Pointer(s_emptyScript, value.ToObject<int>());
                            break;
                        default:
                            break;
                    }
                    return s;
                }
            }
            throw new FormatException();
        }

        public static JToken StackItemToJToken(StackItem item, IList<(StackItem, JToken?)>? context, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            JToken? o = null;
            switch (item)
            {
                case Struct @struct:
                    if (context is null)
                        context = new List<(StackItem, JToken?)>();
                    else
                        (_, o) = context.FirstOrDefault(f => ReferenceEquals(f.Item1, item));
                    if (o is null)
                    {
                        context.Add((item, o));
                        var a = @struct.Select(s => StackItemToJToken(s, context, serializer));
                        o = JToken.FromObject(new
                        {
                            Type = StackItemType.Struct.ToString(),
                            Value = JArray.FromObject(a),
                        }, serializer);
                    }
                    break;
                case Array array:
                    if (context is null)
                        context = new List<(StackItem, JToken?)>();
                    else
                        (_, o) = context.FirstOrDefault(f => ReferenceEquals(f.Item1, item));
                    if (o is null)
                    {
                        context.Add((item, o));
                        var a = array.Select(s => StackItemToJToken(s, context, serializer));
                        o = JToken.FromObject(new
                        {
                            Type = StackItemType.Array.ToString(),
                            Value = JArray.FromObject(a, serializer),
                        }, serializer);
                    }
                    break;
                case Map map:
                    if (context is null)
                        context = new List<(StackItem, JToken?)>();
                    else
                        (_, o) = context.FirstOrDefault(f => ReferenceEquals(f.Item1, item));
                    if (o is null)
                    {
                        context.Add((item, o));
                        var kvp = map.Select(s =>
                            new KeyValuePair<JToken, JToken>(
                                StackItemToJToken(s.Key, context, serializer),
                                StackItemToJToken(s.Value, context, serializer)));
                        o = JToken.FromObject(new
                        {
                            Type = StackItemType.Map.ToString(),
                            Value = JArray.FromObject(kvp, serializer),
                        }, serializer);
                    }
                    break;
                case Boolean:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.Boolean.ToString(),
                        Value = item.GetBoolean(),
                    }, serializer);
                    break;
                case Buffer:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.Buffer.ToString(),
                        Value = Convert.ToBase64String(item.GetSpan()),
                    }, serializer);
                    break;
                case ByteString:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.ByteString.ToString(),
                        Value = Convert.ToBase64String(item.GetSpan()),
                    }, serializer);
                    break;
                case Integer:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.Integer.ToString(),
                        Value = item.GetInteger(),
                    }, serializer);
                    break;
                case InteropInterface:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.InteropInterface.ToString(),
                        Value = Convert.ToBase64String(item.GetSpan()),
                    });
                    break;
                case Pointer pointer:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.Pointer.ToString(),
                        Value = pointer.Position,
                    }, serializer);
                    break;
                case Null:
                case null:
                    o = JToken.FromObject(new
                    {
                        Type = StackItemType.Any.ToString(),
                        Value = JValue.CreateNull(),
                    }, serializer);
                    break;
                default:
                    throw new NotSupportedException($"StackItemType({item.Type}) is not supported to JSON.");
            }
            return o;
        }

        public static InvokeParams ContractInvokeParametersFromJToken(JToken token)
        {
            if (token is null)
                throw new ArgumentNullException();
            if (token.Type != JTokenType.Object)
                throw new FormatException();

            var obj = (JObject)token;
            var contractParametersProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("contractParameters", StringComparison.InvariantCultureIgnoreCase));
            var signersProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("signers", StringComparison.InvariantCultureIgnoreCase));

            return new()
            {
                ContractParameters = [.. contractParametersProp!.Value.Select(ContractParameterFromJToken)],
                Signers = [.. signersProp!.Value.Select(SignerFromJToken)],
            };
        }

        public static Signer SignerFromJToken(JToken? token)
        {
            if (token is null)
                throw new ArgumentNullException();
            if (token.Type != JTokenType.Object)
                throw new FormatException();

            var obj = (JObject)token;
            var accountProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("account", StringComparison.InvariantCultureIgnoreCase));
            var scopesProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("scopes", StringComparison.InvariantCultureIgnoreCase));

            if (accountProp == null || scopesProp == null)
                throw new FormatException();

            return new()
            {
                Account = UInt160.Parse(accountProp.ToObject<string>()),
                Scopes = Enum.Parse<WitnessScope>(scopesProp.ToObject<string>()!),
            };
        }

        public static ContractParameter ContractParameterFromJToken(JToken? token)
        {
            if (token is null)
                throw new ArgumentNullException();
            if (token.Type != JTokenType.Object)
                throw new FormatException();

            var obj = (JObject)token;
            var typeProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("type", StringComparison.InvariantCultureIgnoreCase));
            var valueProp = obj
                .Properties()
                .SingleOrDefault(a => a.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase));

            if (typeProp == null || valueProp == null)
                throw new FormatException();

            var typeValue = Enum.Parse<ContractParameterType>(typeProp.ToObject<string>() ?? throw new ArgumentNullException());

            switch (typeValue)
            {
                case ContractParameterType.Any:
                    return new ContractParameter(ContractParameterType.Any);
                case ContractParameterType.ByteArray:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.ByteArray,
                        Value = Convert.FromBase64String(valueProp.ToObject<string>() ?? throw new ArgumentNullException()),
                    };
                case ContractParameterType.Signature:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Signature,
                        Value = Convert.FromBase64String(valueProp.ToObject<string>() ?? throw new ArgumentNullException()),
                    };
                case ContractParameterType.Boolean:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Boolean,
                        Value = valueProp.ToObject<bool>(),
                    };
                case ContractParameterType.Integer:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Integer,
                        Value = BigInteger.Parse(valueProp.ToObject<string>() ?? throw new ArgumentNullException()),
                    };
                case ContractParameterType.String:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.String,
                        Value = valueProp.ToObject<string>(),
                    };
                case ContractParameterType.Hash160:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Hash160,
                        Value = UInt160.Parse(valueProp.ToObject<string>()),
                    };
                case ContractParameterType.Hash256:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Hash256,
                        Value = UInt256.Parse(valueProp.ToObject<string>()),
                    };
                case ContractParameterType.PublicKey:
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.PublicKey,
                        Value = ECPoint.Parse(valueProp.ToObject<string>(), ECCurve.Secp256r1),
                    };
                case ContractParameterType.Array:
                    if (valueProp.Value is not JArray array)
                        throw new FormatException();
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Array,
                        Value = array.Select(ContractParameterFromJToken).ToList(),
                    };
                case ContractParameterType.Map:
                    if (valueProp.Value is not JArray map)
                        throw new FormatException();
                    return new ContractParameter()
                    {
                        Type = ContractParameterType.Map,
                        Value = map.Select(s =>
                        {
                            if (valueProp.Value is not JObject mapProp)
                                throw new FormatException();
                            var keyProp = mapProp
                                .Properties()
                                .SingleOrDefault(ss => ss.Name.Equals("key", StringComparison.InvariantCultureIgnoreCase));
                            var keyValueProp = mapProp
                                .Properties()
                                .SingleOrDefault(ss => ss.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase));
                            return new KeyValuePair<ContractParameter, ContractParameter>(ContractParameterFromJToken(keyProp?.Value), ContractParameterFromJToken(keyValueProp?.Value));
                        }).ToList(),
                    };
                default:
                    throw new NotSupportedException($"ContractParameterType({typeValue}) is not supported to JSON.");
            }
        }
    }
}
