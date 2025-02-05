// Copyright (C) 2015-2025 The Neo Project.
//
// StateRoot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Plugins.StateService.Network
{
    class StateRoot : IVerifiable
    {
        public const byte CurrentVersion = 0x00;

        public byte Version;
        public uint Index;
        public UInt256 RootHash;
        public Witness Witness;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash is null)
                {
                    _hash = this.CalculateHash();
                }
                return _hash;
            }
        }

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException(null, nameof(value));
                Witness = value[0];
            }
        }

        int ISerializable.Size =>
            sizeof(byte) +      //Version
            sizeof(uint) +      //Index
            UInt256.Length +    //RootHash
            (Witness is null ? 1 : 1 + Witness.Size); //Witness

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            DeserializeUnsigned(ref reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            Witness = witnesses.Length switch
            {
                0 => null,
                1 => witnesses[0],
                _ => throw new FormatException(),
            };
        }

        public void DeserializeUnsigned(ref MemoryReader reader)
        {
            Version = reader.ReadByte();
            Index = reader.ReadUInt32();
            RootHash = reader.ReadSerializable<UInt256>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            SerializeUnsigned(writer);
            if (Witness is null)
                writer.WriteVarInt(0);
            else
                writer.Write(new[] { Witness });
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Index);
            writer.Write(RootHash);
        }

        public bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            return this.VerifyWitnesses(settings, snapshot, 2_00000000L);
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
        {
            ECPoint[] validators = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.StateValidator, Index);
            if (validators.Length < 1) throw new InvalidOperationException("No script hash for state root verifying");
            return new UInt160[] { Contract.GetBFTAddress(validators) };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["version"] = Version;
            json["index"] = Index;
            json["roothash"] = RootHash.ToString();
            json["witnesses"] = Witness is null ? new JArray() : new JArray(Witness.ToJson());
            return json;
        }
    }
}
