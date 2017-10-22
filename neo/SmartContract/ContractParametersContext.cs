using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    public class ContractParametersContext
    {
        private class ContextItem
        {
            public byte[] Script;
            public ContractParameter[] Parameters;
            public Dictionary<ECPoint, byte[]> Signatures;

            private ContextItem() { }

            public ContextItem(Contract contract)
            {
                this.Script = contract.Script;
                this.Parameters = contract.ParameterList.Select(p => new ContractParameter { Type = p }).ToArray();
            }

            public static ContextItem FromJson(JObject json)
            {
                return new ContextItem
                {
                    Script = json["script"]?.AsString().HexToBytes(),
                    Parameters = ((JArray)json["parameters"]).Select(p => ContractParameter.FromJson(p)).ToArray(),
                    Signatures = json["signatures"]?.Properties.Select(p => new
                    {
                        PublicKey = ECPoint.Parse(p.Key, ECCurve.Secp256r1),
                        Signature = p.Value.AsString().HexToBytes()
                    }).ToDictionary(p => p.PublicKey, p => p.Signature)
                };
            }

            public JObject ToJson()
            {
                JObject json = new JObject();
                if (Script != null)
                    json["script"] = Script.ToHexString();
                json["parameters"] = new JArray(Parameters.Select(p => p.ToJson()));
                if (Signatures != null)
                {
                    json["signatures"] = new JObject();
                    foreach (var signature in Signatures)
                        json["signatures"][signature.Key.ToString()] = signature.Value.ToHexString();
                }
                return json;
            }
        }

        public readonly IVerifiable Verifiable;
        private readonly Dictionary<UInt160, ContextItem> ContextItems;

        public bool Completed
        {
            get
            {
                if (ContextItems.Count < ScriptHashes.Count)
                    return false;
                return ContextItems.Values.All(p => p != null && p.Parameters.All(q => q.Value != null));
            }
        }

        private UInt160[] _ScriptHashes = null;
        public IReadOnlyList<UInt160> ScriptHashes
        {
            get
            {
                if (_ScriptHashes == null)
                {
                    _ScriptHashes = Verifiable.GetScriptHashesForVerifying();
                }
                return _ScriptHashes;
            }
        }

        public ContractParametersContext(IVerifiable verifiable)
        {
            this.Verifiable = verifiable;
            this.ContextItems = new Dictionary<UInt160, ContextItem>();
        }

        public bool Add(Contract contract, int index, object parameter)
        {
            ContextItem item = CreateItem(contract);
            if (item == null) return false;
            item.Parameters[index].Value = parameter;
            return true;
        }

        public bool AddSignature(Contract contract, ECPoint pubkey, byte[] signature)
        {
            if (contract.IsMultiSigContract())
            {
                ContextItem item = CreateItem(contract);
                if (item == null) return false;
                if (item.Parameters.All(p => p.Value != null)) return false;
                if (item.Signatures == null)
                    item.Signatures = new Dictionary<ECPoint, byte[]>();
                else if (item.Signatures.ContainsKey(pubkey))
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
                item.Signatures.Add(pubkey, signature);
                if (item.Signatures.Count == contract.ParameterList.Length)
                {
                    Dictionary<ECPoint, int> dic = points.Select((p, i) => new
                    {
                        PublicKey = p,
                        Index = i
                    }).ToDictionary(p => p.PublicKey, p => p.Index);
                    byte[][] sigs = item.Signatures.Select(p => new
                    {
                        Signature = p.Value,
                        Index = dic[p.Key]
                    }).OrderByDescending(p => p.Index).Select(p => p.Signature).ToArray();
                    for (int i = 0; i < sigs.Length; i++)
                        if (!Add(contract, i, sigs[i]))
                            throw new InvalidOperationException();
                    item.Signatures = null;
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

                if(index == -1) {
                    // unable to find ContractParameterType.Signature in contract.ParameterList 
                    // return now to prevent array index out of bounds exception
                    return false;
                }
                return Add(contract, index, signature);
            }
        }

        private ContextItem CreateItem(Contract contract)
        {
            if (ContextItems.TryGetValue(contract.ScriptHash, out ContextItem item))
                return item;
            if (!ScriptHashes.Contains(contract.ScriptHash))
                return null;
            item = new ContextItem(contract);
            ContextItems.Add(contract.ScriptHash, item);
            return item;
        }

        public static ContractParametersContext FromJson(JObject json)
        {
            IVerifiable verifiable = typeof(ContractParametersContext).GetTypeInfo().Assembly.CreateInstance(json["type"].AsString()) as IVerifiable;
            if (verifiable == null) throw new FormatException();
            using (MemoryStream ms = new MemoryStream(json["hex"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                verifiable.DeserializeUnsigned(reader);
            }
            ContractParametersContext context = new ContractParametersContext(verifiable);
            foreach (var property in json["items"].Properties)
            {
                context.ContextItems.Add(UInt160.Parse(property.Key), ContextItem.FromJson(property.Value));
            }
            return context;
        }

        public ContractParameter GetParameter(UInt160 scriptHash, int index)
        {
            return GetParameters(scriptHash)?[index];
        }

        public IReadOnlyList<ContractParameter> GetParameters(UInt160 scriptHash)
        {
            if (!ContextItems.TryGetValue(scriptHash, out ContextItem item))
                return null;
            return item.Parameters;
        }

        public Witness[] GetScripts()
        {
            if (!Completed) throw new InvalidOperationException();
            Witness[] scripts = new Witness[ScriptHashes.Count];
            for (int i = 0; i < ScriptHashes.Count; i++)
            {
                ContextItem item = ContextItems[ScriptHashes[i]];
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    foreach (ContractParameter parameter in item.Parameters.Reverse())
                    {
                        sb.EmitPush(parameter);
                    }
                    scripts[i] = new Witness
                    {
                        InvocationScript = sb.ToArray(),
                        VerificationScript = item.Script ?? new byte[0]
                    };
                }
            }
            return scripts;
        }

        public static ContractParametersContext Parse(string value)
        {
            return FromJson(JObject.Parse(value));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = Verifiable.GetType().FullName;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Verifiable.SerializeUnsigned(writer);
                writer.Flush();
                json["hex"] = ms.ToArray().ToHexString();
            }
            json["items"] = new JObject();
            foreach (var item in ContextItems)
                json["items"][item.Key.ToString()] = item.Value.ToJson();
            return json;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}
