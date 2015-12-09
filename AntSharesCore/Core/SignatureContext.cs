using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AntShares.Core
{
    public class SignatureContext
    {
        public readonly ISignable Signable;
        public readonly UInt160[] ScriptHashes;
        private readonly byte[][] redeemScripts;
        private readonly Dictionary<ECPoint, byte[]>[] signatures;
        private readonly bool[] completed;

        public bool Completed
        {
            get
            {
                return completed.All(p => p);
            }
        }

        public SignatureContext(ISignable signable)
        {
            this.Signable = signable;
            this.ScriptHashes = signable.GetScriptHashesForVerifying();
            this.redeemScripts = new byte[ScriptHashes.Length][];
            this.signatures = new Dictionary<ECPoint, byte[]>[ScriptHashes.Length];
            this.completed = new bool[ScriptHashes.Length];
        }

        public bool Add(Contract contract, ECPoint pubkey, byte[] signature)
        {
            for (int i = 0; i < ScriptHashes.Length; i++)
            {
                if (ScriptHashes[i] == contract.ScriptHash)
                {
                    if (redeemScripts[i] == null)
                        redeemScripts[i] = contract.RedeemScript;
                    if (signatures[i] == null)
                        signatures[i] = new Dictionary<ECPoint, byte[]>();
                    if (signatures[i].ContainsKey(pubkey))
                        signatures[i][pubkey] = signature;
                    else
                        signatures[i].Add(pubkey, signature);
                    Check(contract);
                    return true;
                }
            }
            return false;
        }

        public void Check(Contract contract)
        {
            for (int i = 0; i < ScriptHashes.Length; i++)
            {
                if (ScriptHashes[i] == contract.ScriptHash)
                {
                    completed[i] = contract.IsCompleted(signatures[i].Keys.ToArray());
                    break;
                }
            }
        }

        public Script[] GetScripts()
        {
            if (!Completed) throw new InvalidOperationException();
            Script[] scripts = new Script[signatures.Length];
            for (int i = 0; i < scripts.Length; i++)
            {
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    foreach (byte[] signature in signatures[i].OrderBy(p => p.Key).Select(p => p.Value))
                    {
                        sb.Push(signature);
                    }
                    scripts[i] = new Script
                    {
                        StackScript = sb.ToArray(),
                        RedeemScript = redeemScripts[i]
                    };
                }
            }
            return scripts;
        }

        public static SignatureContext Parse(string value)
        {
            JObject json = JObject.Parse(value);
            string typename = string.Format("{0}.{1}", typeof(SignatureContext).Namespace, json["type"].AsString());
            ISignable signable = Assembly.GetExecutingAssembly().CreateInstance(typename) as ISignable;
            using (MemoryStream ms = new MemoryStream(json["hex"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                signable.DeserializeUnsigned(reader);
            }
            SignatureContext context = new SignatureContext(signable);
            JArray scripts = (JArray)json["scripts"];
            for (int i = 0; i < scripts.Count; i++)
            {
                if (scripts[i] != null)
                {
                    context.redeemScripts[i] = scripts[i]["redeem_script"].AsString().HexToBytes();
                    context.signatures[i] = new Dictionary<ECPoint, byte[]>();
                    JArray sigs = (JArray)scripts[i]["signatures"];
                    for (int j = 0; j < sigs.Count; j++)
                    {
                        ECPoint pubkey = ECPoint.DecodePoint(sigs[j]["pubkey"].AsString().HexToBytes(), ECCurve.Secp256r1);
                        byte[] signature = sigs[j]["signature"].AsString().HexToBytes();
                        context.signatures[i].Add(pubkey, signature);
                    }
                    context.completed[i] = scripts[i]["completed"].AsBoolean();
                }
            }
            return context;
        }

        public override string ToString()
        {
            JObject json = new JObject();
            json["type"] = Signable.GetType().Name;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Signable.SerializeUnsigned(writer);
                writer.Flush();
                json["hex"] = ms.ToArray().ToHexString();
            }
            JArray scripts = new JArray();
            for (int i = 0; i < signatures.Length; i++)
            {
                if (signatures[i] == null)
                {
                    scripts.Add(null);
                }
                else
                {
                    scripts.Add(new JObject());
                    scripts[i]["redeem_script"] = redeemScripts[i].ToHexString();
                    JArray sigs = new JArray();
                    foreach (var pair in signatures[i])
                    {
                        JObject signature = new JObject();
                        signature["pubkey"] = pair.Key.EncodePoint(true).ToHexString();
                        signature["signature"] = pair.Value.ToHexString();
                        sigs.Add(signature);
                    }
                    scripts[i]["signatures"] = sigs;
                    scripts[i]["completed"] = completed[i];
                }
            }
            json["scripts"] = scripts;
            return json.ToString();
        }
    }
}
