using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Native
{
    public sealed class RoleManagement : NativeContract
    {
        private const byte Prefix_VoteContexts = 0x01;
        private const byte Prefix_Index = 0x02;

        internal RoleManagement()
        {
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public ECPoint[] GetDesignatedByRole(DataCache snapshot, Role role, uint index)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (Ledger.CurrentIndex(snapshot) + 1 < index)
                throw new ArgumentOutOfRangeException(nameof(index));
            byte[] key = CreateStorageKey((byte)role).Add(Prefix_Index).AddBigEndian(index).ToArray();
            byte[] boundary = CreateStorageKey((byte)role).Add(Prefix_Index).ToArray();
            return snapshot.FindRange(key, boundary, SeekDirection.Backward)
                .Select(u => u.Value.Value.AsSerializableArray<ECPoint>())
                .FirstOrDefault() ?? System.Array.Empty<ECPoint>();
        }

        [ContractMethod(0, CallFlags.WriteStates)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > 32)
                throw new ArgumentException(nameof(nodes));
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (engine.PersistingBlock is null)
                throw new InvalidOperationException(nameof(DesignateAsRole));
            var committee = NEO.GetCommittee(engine.Snapshot);
            var sender = ((Transaction)engine.GetScriptContainer()).Sender;
            ECPoint member = null;
            foreach (var m in committee)
            {
                if (m.EncodePoint(true).ToScriptHash() == sender)
                    member = m;
            }
            if (member is null)
                throw new InvalidOperationException("Permission deny");
            if (!engine.CheckWitness(sender.ToArray()))
                return;
            var nl = new NodeList(nodes);
            var key = CreateStorageKey((byte)role).Add(Prefix_VoteContexts).Add(nl.Hash);
            var item = engine.Snapshot.GetAndChange(key);
            if (item is null)
            {
                var context = new VoteContext
                {
                    Height = Ledger.CurrentIndex(engine.Snapshot),
                    Nodes = nl,
                    Voters = new List<ECPoint> { member },
                };
                item = new StorageItem(context.ToArray());
                engine.Snapshot.Add(key, item);
            }
            else
            {
                var context = item.Value.AsSerializable<VoteContext>();
                if (context.Voters.Contains(member)) return;
                if (committee.Length / 2 + 1 <= context.Voters.Count) return;
                context.Voters.Add(member);
                item = new StorageItem(context.ToArray());
                if (committee.Length / 2 + 1 <= context.Voters.Count)
                {
                    uint index = engine.PersistingBlock.Index + 1;
                    var index_key = CreateStorageKey((byte)role).Add(Prefix_Index).AddBigEndian(index);
                    if (engine.Snapshot.Contains(key))
                        throw new InvalidOperationException();
                    var list = new NodeList(nodes);
                    engine.Snapshot.Add(key, new StorageItem(IO.Helper.ToArray(list)));
                }
            }
        }

        private class VoteContext : ISerializable
        {
            public uint Height;
            public NodeList Nodes;
            public List<ECPoint> Voters;

            public int Size => sizeof(uint) + Nodes.GetVarSize() + Voters.GetVarSize();

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Height);
                writer.Write(Nodes.ToArray());
                writer.Write(Voters.ToArray());
            }

            public void Deserialize(BinaryReader reader)
            {
                Height = reader.ReadUInt32();
                Nodes = new NodeList(reader.ReadSerializableArray<ECPoint>());
                Voters = reader.ReadSerializableArray<ECPoint>().ToList();
            }
        }

        private class NodeList : List<ECPoint>, ISerializable
        {
            private UInt256 hash;
            public int Size => this.GetVarSize();
            public UInt256 Hash
            {
                get
                {
                    if (hash is null)
                        hash = new UInt256(IO.Helper.ToArray(this).Sha256());
                    return hash;
                }
            }

            public NodeList(IEnumerable<ECPoint> nodes)
            {
                AddRange(nodes);
                Sort();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(ToArray());
            }

            public void Deserialize(BinaryReader reader)
            {
                AddRange(reader.ReadSerializableArray<ECPoint>());
            }
        }
    }
}
