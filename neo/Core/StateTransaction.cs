using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class StateTransaction : Transaction
    {
        public StateDescriptor[] Descriptors;

        public override int Size => base.Size + Descriptors.GetVarSize();
        public override Fixed8 SystemFee => Descriptors.Sum(p => p.SystemFee);

        public StateTransaction()
            : base(TransactionType.StateTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Descriptors = reader.ReadSerializableArray<StateDescriptor>(16);
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (StateDescriptor descriptor in Descriptors)
            {
                switch (descriptor.Type)
                {
                    case StateType.Account:
                        hashes.UnionWith(GetScriptHashesForVerifying_Account(descriptor));
                        break;
                    case StateType.Validator:
                        hashes.UnionWith(GetScriptHashesForVerifying_Validator(descriptor));
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        private IEnumerable<UInt160> GetScriptHashesForVerifying_Account(StateDescriptor descriptor)
        {
            switch (descriptor.Field)
            {
                case "Votes":
                    yield return new UInt160(descriptor.Key);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator(StateDescriptor descriptor)
        {
            switch (descriptor.Field)
            {
                case "Registered":
                    yield return Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1)).ToScriptHash();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Descriptors);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["descriptors"] = new JArray(Descriptors.Select(p => p.ToJson()));
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            foreach (StateDescriptor descriptor in Descriptors)
                if (!descriptor.Verify())
                    return false;
            return base.Verify(mempool);
        }
    }
}
