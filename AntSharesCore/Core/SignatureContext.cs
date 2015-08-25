using AntShares.Cryptography;
using AntShares.IO.Json;
using System;
using System.Linq;
using System.Reflection;

namespace AntShares.Core
{
    public class SignatureContext
    {
        public readonly ISignable Signable;
        public readonly UInt160[] ScriptHashes;
        private MultiSigContext[] signatures;

        public bool Completed
        {
            get
            {
                return signatures.All(p => p != null && p.Completed);
            }
        }

        public SignatureContext(ISignable signable)
        {
            this.Signable = signable;
            this.ScriptHashes = signable.GetScriptHashesForVerifying();
            this.signatures = new MultiSigContext[ScriptHashes.Length];
        }

        public bool Add(byte[] redeemScript, Secp256r1Point pubkey, byte[] signature)
        {
            UInt160 scriptHash = redeemScript.ToScriptHash();
            for (int i = 0; i < ScriptHashes.Length; i++)
            {
                if (ScriptHashes[i] == scriptHash)
                {
                    if (signatures[i] == null)
                        signatures[i] = new MultiSigContext(redeemScript);
                    return signatures[i].Add(pubkey, signature);
                }
            }
            return false;
        }

        public byte[][] GetScripts()
        {
            if (!Completed)
                throw new InvalidOperationException();
            return signatures.Select(p => p.GetScript()).ToArray();
        }

        public static SignatureContext Parse(string value)
        {
            JObject json = JObject.Parse(value);
            string typename = string.Format("{0}.{1}", typeof(SignatureContext).Namespace, json["type"].AsString());
            ISignable signable = Assembly.GetExecutingAssembly().CreateInstance(typename) as ISignable;
            signable.FromUnsignedArray(json["hex"].AsString().HexToBytes());
            SignatureContext context = new SignatureContext(signable);
            JArray multisignatures = (JArray)json["multi_signatures"];
            for (int i = 0; i < multisignatures.Count; i++)
            {
                if (multisignatures[i] != null)
                {
                    context.signatures[i] = new MultiSigContext(multisignatures[i]["redeem_script"].AsString().HexToBytes());
                    JArray sigs = (JArray)multisignatures[i]["signatures"];
                    for (int j = 0; j < sigs.Count; j++)
                    {
                        if (sigs[j] != null)
                        {
                            context.signatures[i].signatures[j] = sigs[j].AsString().HexToBytes();
                        }
                    }
                }
            }
            return context;
        }

        public override string ToString()
        {
            JObject json = new JObject();
            json["type"] = Signable.GetType().Name;
            json["hex"] = Signable.ToUnsignedArray().ToHexString();
            JArray multisignatures = new JArray();
            for (int i = 0; i < signatures.Length; i++)
            {
                if (signatures[i] == null)
                {
                    multisignatures.Add(null);
                }
                else
                {
                    multisignatures.Add(new JObject());
                    multisignatures[i]["redeem_script"] = signatures[i].redeemScript.ToHexString();
                    JArray sigs = new JArray();
                    for (int j = 0; j < signatures[i].signatures.Length; j++)
                    {
                        if (signatures[i].signatures[j] == null)
                        {
                            sigs.Add(null);
                        }
                        else
                        {
                            sigs.Add(signatures[i].signatures[j].ToHexString());
                        }
                    }
                    multisignatures[i]["signatures"] = sigs;
                }
            }
            json["multi_signatures"] = multisignatures;
            return json.ToString();
        }
    }
}
