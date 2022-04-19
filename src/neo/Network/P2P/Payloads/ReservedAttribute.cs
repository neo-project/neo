// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Reserved attribute for dApps.
    /// </summary>
    public class ReservedAttribute : TransactionAttribute
    {
        private byte[] _reserved;

        public override bool AllowMultiple => true;
        public override TransactionAttributeType Type => TransactionAttributeType.ReservedAttribute;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            _reserved = reader.ReadVarBytes(ushort.MaxValue);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.WriteVarBytes(_reserved);
        }

        public override JObject ToJson()
        {
            var json = base.ToJson();
            json["value"] = Convert.ToBase64String(_reserved);
            return json;
        }
    }
}
