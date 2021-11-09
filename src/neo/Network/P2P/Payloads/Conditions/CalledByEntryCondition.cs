// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class CalledByEntryCondition : WitnessCondition
    {
        public override WitnessConditionType Type => WitnessConditionType.CalledByEntry;

        protected override void DeserializeWithoutType(BinaryReader reader, int maxNestDepth)
        {
        }

        public override bool Match(ApplicationEngine engine)
        {
            return engine.CallingScriptHash is null || engine.CallingScriptHash == engine.EntryScriptHash;
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
        }
    }
}
