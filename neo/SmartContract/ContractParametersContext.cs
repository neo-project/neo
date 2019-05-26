using Neo.Cryptography.ECC;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.SmartContract
{
    public class ContractParametersContext
    {
        public readonly Transaction Transaction;
        private ContractParameter[] Parameters;
        private Dictionary<ECPoint, byte[]> Signatures;

        public bool Completed
        {
            get
            {
                if (Parameters is null) return false;
                return Parameters.All(p => p.Value != null);
            }
        }

        public UInt160 ScriptHash => Transaction.SenderHash;

        public ContractParametersContext(Transaction tx)
        {
            this.Transaction = tx;
        }

        public bool Add(Contract contract, int index, object parameter)
        {
            if (!ScriptHash.Equals(contract.ScriptHash)) return false;
            if (Parameters is null)
            {
                Parameters = contract.ParameterList.Select(p => new ContractParameter { Type = p }).ToArray();
            }
            Parameters[index].Value = parameter;
            return true;
        }

        public bool AddSignature(Contract contract, ECPoint pubkey, byte[] signature)
        {
            if (contract.Script.IsMultiSigContract())
            {
                if (!ScriptHash.Equals(contract.ScriptHash)) return false;
                if (Parameters is null)
                {
                    Parameters = contract.ParameterList.Select(p => new ContractParameter { Type = p }).ToArray();
                }
                if (Parameters.All(p => p.Value != null)) return false;
                if (Signatures == null)
                    Signatures = new Dictionary<ECPoint, byte[]>();
                else if (Signatures.ContainsKey(pubkey))
                    return false;
                List<ECPoint> points = new List<ECPoint>();
                {
                    int i = 0;
                    switch (contract.Script[i++])
                    {
                        case 1:
                            ++i;
                            break;
                        case 2:
                            i += 2;
                            break;
                    }
                    while (contract.Script[i++] == 33)
                    {
                        points.Add(ECPoint.DecodePoint(contract.Script.Skip(i).Take(33).ToArray(), ECCurve.Secp256r1));
                        i += 33;
                    }
                }
                if (!points.Contains(pubkey)) return false;
                Signatures.Add(pubkey, signature);
                if (Signatures.Count == contract.ParameterList.Length)
                {
                    Dictionary<ECPoint, int> dic = points.Select((p, i) => new
                    {
                        PublicKey = p,
                        Index = i
                    }).ToDictionary(p => p.PublicKey, p => p.Index);
                    byte[][] sigs = Signatures.Select(p => new
                    {
                        Signature = p.Value,
                        Index = dic[p.Key]
                    }).OrderByDescending(p => p.Index).Select(p => p.Signature).ToArray();
                    for (int i = 0; i < sigs.Length; i++)
                        if (!Add(contract, i, sigs[i]))
                            throw new InvalidOperationException();
                    Signatures = null;
                }
                return true;
            }
            else
            {
                int index = -1;
                for (int i = 0; i < contract.ParameterList.Length; i++)
                    if (contract.ParameterList[i] == ContractParameterType.Signature)
                        if (index >= 0)
                            throw new NotSupportedException();
                        else
                            index = i;

                if (index == -1)
                {
                    // unable to find ContractParameterType.Signature in contract.ParameterList 
                    // return now to prevent array index out of bounds exception
                    return false;
                }
                return Add(contract, index, signature);
            }
        }

        public static ContractParametersContext FromJson(JObject json)
        {
            Transaction tx = new Transaction();
            using (MemoryStream ms = new MemoryStream(json["hex"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                tx.DeserializeUnsigned(reader);
            }
            return new ContractParametersContext(tx)
            {
                Parameters = ((JArray)json["parameters"])?.Select(p => ContractParameter.FromJson(p)).ToArray(),
                Signatures = json["signatures"]?.Properties.Select(p => new
                {
                    PublicKey = ECPoint.Parse(p.Key, ECCurve.Secp256r1),
                    Signature = p.Value.AsString().HexToBytes()
                }).ToDictionary(p => p.PublicKey, p => p.Signature)
            };
        }

        public ContractParameter GetParameter(int index)
        {
            return GetParameters()?[index];
        }

        public IReadOnlyList<ContractParameter> GetParameters()
        {
            return Parameters;
        }

        public byte[] GetWitness()
        {
            if (!Completed) throw new InvalidOperationException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (ContractParameter parameter in Parameters.Reverse())
                {
                    sb.EmitPush(parameter);
                }
                return sb.ToArray();
            }
        }

        public static ContractParametersContext Parse(string value)
        {
            return FromJson(JObject.Parse(value));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hex"] = Transaction.GetHashData().ToHexString();
            if (Parameters != null)
                json["parameters"] = new JArray(Parameters.Select(p => p.ToJson()));
            if (Signatures != null)
            {
                json["signatures"] = new JObject();
                foreach (var signature in Signatures)
                    json["signatures"][signature.Key.ToString()] = signature.Value.ToHexString();
            }
            return json;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}
