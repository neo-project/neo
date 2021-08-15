// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents an extensible message that can be relayed.
    /// </summary>
    public class ExtensiblePayload : IInventory
    {
        /// <summary>
        /// The category of the extension.
        /// </summary>
        public string Category;

        /// <summary>
        /// Indicates that the payload is only valid when the block height is greater than or equal to this value.
        /// </summary>
        public uint ValidBlockStart;

        /// <summary>
        /// Indicates that the payload is only valid when the block height is less than this value.
        /// </summary>
        public uint ValidBlockEnd;

        /// <summary>
        /// The sender of the payload.
        /// </summary>
        public UInt160 Sender;

        /// <summary>
        /// The data of the payload.
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// The witness of the payload. It must match the <see cref="Sender"/>.
        /// </summary>
        public Witness Witness;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = this.CalculateHash();
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Extensible;

        public int Size =>
            Category.GetVarSize() + //Category
            sizeof(uint) +          //ValidBlockStart
            sizeof(uint) +          //ValidBlockEnd
            UInt160.Length +        //Sender
            Data.GetVarSize() +     //Data
            1 + Witness.Size;       //Witness

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Witness = value[0];
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Witness = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Category = reader.ReadVarString(32);
            ValidBlockStart = reader.ReadUInt32();
            ValidBlockEnd = reader.ReadUInt32();
            if (ValidBlockStart >= ValidBlockEnd) throw new FormatException();
            Sender = reader.ReadSerializable<UInt160>();
            Data = reader.ReadVarBytes(Message.PayloadMaxSize);
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            return new[] { Sender }; // This address should be checked by consumer
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.WriteVarString(Category);
            writer.Write(ValidBlockStart);
            writer.Write(ValidBlockEnd);
            writer.Write(Sender);
            writer.WriteVarBytes(Data);
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot, ISet<UInt160> extensibleWitnessWhiteList)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (height < ValidBlockStart || height >= ValidBlockEnd) return false;
            if (!extensibleWitnessWhiteList.Contains(Sender)) return false;
            return this.VerifyWitnesses(settings, snapshot, 0_06000000L);
        }
    }
}
