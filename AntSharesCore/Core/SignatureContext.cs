using AntShares.IO.Json;
using System;
using System.Linq;

namespace AntShares.Core
{
    public class SignatureContext
    {
        private ISignable signable;
        private UInt160[] scriptHashes;
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
            this.signable = signable;
            this.scriptHashes = signable.GetScriptHashesForVerifying();
            this.signatures = new MultiSigContext[scriptHashes.Length];
        }

        public bool Add(byte[] redeemScript, UInt160 pubKeyHash, byte[] signature)
        {
            UInt160 scriptHash = redeemScript.ToScriptHash();
            for (int i = 0; i < scriptHashes.Length; i++)
            {
                if (scriptHashes[i] == scriptHash)
                {
                    if (signatures[i] == null)
                        signatures[i] = new MultiSigContext(redeemScript);
                    return signatures[i].Add(pubKeyHash, signature);
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

        public override string ToString()
        {
            JObject json = new JObject();
            json["type"] = signable.GetType().Name;
            json["hex"] = signable.ToUnsignedArray().ToHexString();
            JArray multisignatures = new JArray();
            for (int i = 0; i < signatures.Length; i++)
            {
                if (signatures[i] == null)
                {
                    multisignatures.Add(JNull.Value);
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
                            sigs.Add(JNull.Value);
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
