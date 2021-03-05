using Neo.Cryptography.ECC;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.SmartContract
{
    public class ContractParameter
    {
        public ContractParameterType Type;
        public object Value;

        public ContractParameter() { }

        public ContractParameter(ContractParameterType type)
        {
            this.Type = type;
            this.Value = type switch
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
                _ => throw new ArgumentException(),
            };
        }

        public static ContractParameter FromJson(JObject json)
        {
            ContractParameter parameter = new ContractParameter
            {
                Type = Enum.Parse<ContractParameterType>(json["type"].GetString())
            };
            if (json["value"] != null)
                switch (parameter.Type)
                {
                    case ContractParameterType.Signature:
                    case ContractParameterType.ByteArray:
                        parameter.Value = Convert.FromBase64String(json["value"].AsString());
                        break;
                    case ContractParameterType.Boolean:
                        parameter.Value = json["value"].AsBoolean();
                        break;
                    case ContractParameterType.Integer:
                        parameter.Value = BigInteger.Parse(json["value"].AsString());
                        break;
                    case ContractParameterType.Hash160:
                        parameter.Value = UInt160.Parse(json["value"].AsString());
                        break;
                    case ContractParameterType.Hash256:
                        parameter.Value = UInt256.Parse(json["value"].AsString());
                        break;
                    case ContractParameterType.PublicKey:
                        parameter.Value = ECPoint.Parse(json["value"].AsString(), ECCurve.Secp256r1);
                        break;
                    case ContractParameterType.String:
                        parameter.Value = json["value"].AsString();
                        break;
                    case ContractParameterType.Array:
                        parameter.Value = ((JArray)json["value"]).Select(p => FromJson(p)).ToList();
                        break;
                    case ContractParameterType.Map:
                        parameter.Value = ((JArray)json["value"]).Select(p => new KeyValuePair<ContractParameter, ContractParameter>(FromJson(p["key"]), FromJson(p["value"]))).ToList();
                        break;
                    default:
                        throw new ArgumentException();
                }
            return parameter;
        }

        public void SetValue(string text)
        {
            switch (Type)
            {
                case ContractParameterType.Signature:
                    byte[] signature = text.HexToBytes();
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
                    Value = text.HexToBytes();
                    break;
                case ContractParameterType.PublicKey:
                    Value = ECPoint.Parse(text, ECCurve.Secp256r1);
                    break;
                case ContractParameterType.String:
                    Value = text;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public JObject ToJson()
        {
            return ToJson(this, null);
        }

        private static JObject ToJson(ContractParameter parameter, HashSet<ContractParameter> context)
        {
            JObject json = new JObject();
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
                        if (context is null)
                            context = new HashSet<ContractParameter>();
                        else if (context.Contains(parameter))
                            throw new InvalidOperationException();
                        context.Add(parameter);
                        json["value"] = new JArray(((IList<ContractParameter>)parameter.Value).Select(p => ToJson(p, context)));
                        break;
                    case ContractParameterType.Map:
                        if (context is null)
                            context = new HashSet<ContractParameter>();
                        else if (context.Contains(parameter))
                            throw new InvalidOperationException();
                        context.Add(parameter);
                        json["value"] = new JArray(((IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value).Select(p =>
                        {
                            JObject item = new JObject();
                            item["key"] = ToJson(p.Key, context);
                            item["value"] = ToJson(p.Value, context);
                            return item;
                        }));
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
                        StringBuilder sb = new StringBuilder();
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
                        StringBuilder sb = new StringBuilder();
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
