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
            switch (type)
            {
                case ContractParameterType.Signature:
                    this.Value = new byte[64];
                    break;
                case ContractParameterType.Boolean:
                    this.Value = false;
                    break;
                case ContractParameterType.Integer:
                    this.Value = 0;
                    break;
                case ContractParameterType.Hash160:
                    this.Value = new UInt160();
                    break;
                case ContractParameterType.Hash256:
                    this.Value = new UInt256();
                    break;
                case ContractParameterType.ByteArray:
                    this.Value = new byte[0];
                    break;
                case ContractParameterType.PublicKey:
                    this.Value = ECCurve.Secp256r1.G;
                    break;
                case ContractParameterType.String:
                    this.Value = "";
                    break;
                case ContractParameterType.Array:
                    this.Value = new List<ContractParameter>();
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public static ContractParameter FromJson(JObject json)
        {
            ContractParameter parameter = new ContractParameter
            {
                Type = json["type"].AsEnum<ContractParameterType>()
            };
            JObject value = json["value"];
            if (value != null)
                switch (parameter.Type)
                {
                    case ContractParameterType.Signature:
                    case ContractParameterType.ByteArray:
                        parameter.Value = json["value"].AsString().HexToBytes();
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
                        parameter.Value = ((JArray)json["value"]).Select(p => FromJson(p)).ToArray();
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
            JObject json = new JObject();
            json["type"] = Type;
            if (Value != null)
                switch (Type)
                {
                    case ContractParameterType.Signature:
                    case ContractParameterType.ByteArray:
                        json["value"] = ((byte[])Value).ToHexString();
                        break;
                    case ContractParameterType.Boolean:
                        json["value"] = (bool)Value;
                        break;
                    case ContractParameterType.Integer:
                    case ContractParameterType.Hash160:
                    case ContractParameterType.Hash256:
                    case ContractParameterType.PublicKey:
                    case ContractParameterType.String:
                        json["value"] = Value.ToString();
                        break;
                    case ContractParameterType.Array:
                        json["value"] = new JArray(((IList<ContractParameter>)Value).Select(p => p.ToJson()));
                        break;
                }
            return json;
        }

        public override string ToString()
        {
            switch (Value)
            {
                case null:
                    return "(null)";
                case byte[] data:
                    return data.ToHexString();
                case UIntBase data:
                    return $"0x{data}";
                case IList<ContractParameter> data:
                    StringBuilder sb = new StringBuilder();
                    sb.Append('[');
                    foreach (ContractParameter item in data)
                    {
                        sb.Append(item);
                        sb.Append(", ");
                    }
                    if (data.Count > 0)
                        sb.Length -= 2;
                    sb.Append(']');
                    return sb.ToString();
                default:
                    return Value.ToString();
            }
        }
    }
}
