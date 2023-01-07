// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class ScriptHashCondition : WitnessCondition
    {
        /// <summary>
        /// The script hash to be checked.
        /// </summary>
        public UInt160 Hash;

        public override int Size => base.Size + UInt160.Length;
        public override WitnessConditionType Type => WitnessConditionType.ScriptHash;

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            Hash = reader.ReadSerializable<UInt160>();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return engine.CurrentScriptHash == Hash;
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Hash);
        }

        private protected override void ParseJson(JObject json)
        {
            Hash = UInt160.Parse(json["hash"].GetString());
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            return json;
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            var result = (Array)base.ToStackItem(referenceCounter);
            result.Add(Hash.ToArray());
            return result;
        }
    }
}
