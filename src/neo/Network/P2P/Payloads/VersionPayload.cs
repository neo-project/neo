// Copyright (C) 2015-2021 The Neo Project.
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
using Neo.Network.P2P.Capabilities;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Sent when a connection is established.
    /// </summary>
    public class VersionPayload : IVerifiable
    {
        /// <summary>
        /// Indicates the maximum number of capabilities contained in a <see cref="VersionPayload"/>.
        /// </summary>
        public const int MaxCapabilities = 32;

        /// <summary>
        /// The magic number of the network.
        /// </summary>
        public uint Network;

        /// <summary>
        /// The protocol version of the node.
        /// </summary>
        public uint Version;

        /// <summary>
        /// The time when connected to the node.
        /// </summary>
        public uint Timestamp;

        /// <summary>
        /// A random number used to identify the node.
        /// </summary>
        public uint Nonce;

        /// <summary>
        /// A <see cref="string"/> used to identify the client software of the node.
        /// </summary>
        public string UserAgent;
        public ECPoint Node;

        /// <summary>
        /// The capabilities of the node.
        /// </summary>
        public NodeCapability[] Capabilities;
        public Witness Witness;

        public int Size =>
            sizeof(uint) +              // Network
            sizeof(uint) +              // Version
            sizeof(uint) +              // Timestamp
            sizeof(uint) +              // Nonce
            UserAgent.GetVarSize() +    // UserAgent
            Node.Size +
            Capabilities.GetVarSize() +  // Capabilities
            Witness.Size;

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

        /// <summary>
        /// Creates a new instance of the <see cref="VersionPayload"/> class.
        /// </summary>
        /// <param name="network">The magic number of the network.</param>
        /// <param name="nonce">The random number used to identify the node.</param>
        /// <param name="userAgent">The <see cref="string"/> used to identify the client software of the node.</param>
        /// /// <param name="w">The <see cref="string"/> used to sign message.</param>
        /// <param name="capabilities">The capabilities of the node.</param>
        /// <returns></returns>
        public static VersionPayload Create(uint network, uint nonce, string userAgent, Wallet w, params NodeCapability[] capabilities)
        {
            var payload = new VersionPayload
            {
                Network = network,
                Version = LocalNode.ProtocolVersion,
                Timestamp = DateTime.Now.ToTimestamp(),
                Nonce = nonce,
                UserAgent = userAgent,
                Node = w.GetDefaultAccount().GetKey().PublicKey,
                Capabilities = capabilities,
            };
            payload.Sign(w.GetDefaultAccount().GetKey(), network);
            return payload;
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            return new[] { Contract.CreateSignatureRedeemScript(Node).ToScriptHash() };
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Network = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString(1024);
            Node = reader.ReadSerializable<ECPoint>();
            // Capabilities
            Capabilities = new NodeCapability[reader.ReadVarInt(MaxCapabilities)];
            for (int x = 0, max = Capabilities.Length; x < max; x++)
                Capabilities[x] = NodeCapability.DeserializeFrom(reader);
            if (Capabilities.Select(p => p.Type).Distinct().Count() != Capabilities.Length)
                throw new FormatException();
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Witness = reader.ReadSerializable<Witness>();
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Network);
            writer.Write(Version);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(Node);
            writer.Write(Capabilities);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        public bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            var now = DateTime.UtcNow.ToTimestamp();
            if (Timestamp > now || now - Timestamp < 10) return false;
            if (!NativeContract.Policy.IsAllowed(snapshot, Node)) return false;
            return this.VerifyWitnesses(settings, snapshot, 0_06000000L);
        }
    }
}
