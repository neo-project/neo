// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class CalledByGroupCondition : WitnessCondition
    {
        /// <summary>
        /// The group to be checked.
        /// </summary>
        public ECPoint Group;

        public override int Size => base.Size + Group.Size;
        public override WitnessConditionType Type => WitnessConditionType.CalledByGroup;

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            Group = reader.ReadSerializable<ECPoint>();
        }

        public override bool Match(ApplicationEngine engine)
        {
            engine.ValidateCallFlags(CallFlags.ReadStates);
            ContractState contract = NativeContract.ContractManagement.GetContract(engine.Snapshot, engine.CallingScriptHash);
            return contract is not null && contract.Manifest.Groups.Any(p => p.PubKey.Equals(Group));
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Group);
        }

        private protected override void ParseJson(JObject json)
        {
            Group = ECPoint.Parse(json["group"].GetString(), ECCurve.Secp256r1);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["group"] = Group.ToString();
            return json;
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            var result = (Array)base.ToStackItem(referenceCounter);
            result.Add(Group.ToArray());
            return result;
        }
    }
}
