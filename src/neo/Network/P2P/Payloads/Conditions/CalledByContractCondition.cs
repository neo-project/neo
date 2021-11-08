// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class CalledByContractCondition : WitnessCondition
    {
        /// <summary>
        /// The script hash to be checked.
        /// </summary>
        public UInt160 Hash;

        public override int Size => base.Size + UInt160.Length;
        public override WitnessConditionType Type => WitnessConditionType.CalledByContract;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Hash = reader.ReadSerializable<UInt160>();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return engine.CallingScriptHash == Hash;
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Hash);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            return json;
        }
    }
}
