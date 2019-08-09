#pragma warning disable IDE0051
#pragma warning disable IDE0060

using System;
using System.Collections.Generic;
using System.Text;
using Neo.VM;
using Neo.VM.Types;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using System.Linq;
using Neo.Persistence;
using Neo.Cryptography;
using Neo.SmartContract.Native.Tokens;
using VMArray = Neo.VM.Types.Array;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class VoteContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.VoteContract";

        private const byte Prefix_CreateVote = 10;
        private const byte Prefix_Vote = 11;
        private const byte Prefix_AccessControl = 12;

        private StackItem CreateMultiVote(ApplicationEngine engine, VMArray args)
        {
            UInt160 originator = new UInt160(args[0].GetByteArray());
            if (!InteropService.CheckWitness(engine, originator)) return false;
            var tx = engine.ScriptContainer as Transaction;
            CreateState createState = new CreateState
                (tx.Hash,
                engine.CallingScriptHash,
                originator,
                args[1].GetString(), args[2].GetString(),
                ((VMArray)args[3]).Select(p => p.GetBigInteger()).ToArray(),
                true);
            if (RegisterVote(engine.Snapshot, createState))
            {
                return tx.Hash.ToArray();
            }
            else
            {
                return false;
            }
        }

        private StackItem CreateSingleVote(ApplicationEngine engine, VMArray args)
        {
            UInt160 originator = new UInt160(args[0].GetByteArray());
            if (!InteropService.CheckWitness(engine, originator)) return false;
            var tx = engine.ScriptContainer as Transaction;
            CreateState createState = new CreateState
                (tx.Hash, 
                engine.CallingScriptHash,
                originator,
                args[1].GetString(), args[2].GetString(),
                ((VMArray)args[3]).Select(p => p.GetBigInteger()).ToArray(),
                false);
            if (RegisterVote(engine.Snapshot, createState))
            {
                return tx.Hash.ToArray();
            }
            else
            {
                return false;
            }
        }

        private StackItem MultiVote(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null || args[1] == null || args[2] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(CreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            UInt160 voter = new UInt160(args[1].GetByteArray());
            if (!InteropService.CheckWitness(engine, voter)) return false;

            StorageKey AccessKey = CreateStorageKey(Prefix_AccessControl, id);
            StorageItem access_state = engine.Snapshot.Storages.TryGet(AccessKey);
            if (!(access_state is null))
            {
                List<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }

            MultiCandidate candidate = new MultiCandidate();
            if (candidate.SetByteArray(args[2].GetByteArray()))
            {
                VoteState voteState = new VoteState(id, voter,candidate);
                if (AddVote(engine.Snapshot, voteState))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }            
        }

        private StackItem SingleVote(ApplicationEngine engine, VMArray args)
        {
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(CreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            UInt160 voter = new UInt160(args[1].GetByteArray());
            if (!InteropService.CheckWitness(engine, voter)) return false;

            StorageKey AccessKey = CreateStorageKey(Prefix_AccessControl, id);
            StorageItem access_state = engine.Snapshot.Storages.TryGet(AccessKey);
            if (!(access_state is null))
            {
                List<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }

            SingleCandidate candidate = new SingleCandidate();
            if (candidate.SetByteArray(args[2].GetByteArray()))
            {
                VoteState voteState = new VoteState(id, voter, candidate);
                if (AddVote(engine.Snapshot, voteState))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private StackItem GetVoteDetails(ApplicationEngine engine, VMArray args)
        {
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(CreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));
            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_state = engine.Snapshot.Storages.TryGet(create_key);
            if (create_state is null) return null;
            return create_state.Value;
        }

        private StackItem GetStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(CreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            IEnumerable<KeyValuePair<StorageKey, StorageItem>> pairs = engine.Snapshot.Storages.Find(index_key.Key);

            if (pairs.Count() == 0) return false;

            foreach (KeyValuePair<StorageKey, StorageItem> pair in pairs)
            {
                VoteState vote_state = VoteState.FromByteArray(pair.Value.Value);
                StorageKey account_key = new NeoToken().
                vote_state.GetCandidate().GetByteArray();
            }


        }
        private StackItem AccessControl(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null || args[1] == null || args[2] == null ) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(CreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            List<UInt160> newVoter = ConvertBytesToUserArray(args[1].GetByteArray());

            bool IsAdd = args[2].GetBoolean();
            if (IsAdd)
            {
                try
                {
                    StorageKey key = CreateStorageKey(Prefix_AccessControl, id);
                    StorageItem storage_Access = engine.Snapshot.Storages.GetAndChange(key);
                    List<UInt160> oldVoter = ConvertBytesToUserArray(storage_Access.Value);
                    newVoter.AddRange(oldVoter);
                    storage_Access.Value = ConvertUserArrayToBytes(newVoter);
                }
                catch
                {
                    return false;
                }
                return true;
            }
            else
            {
                try
                {
                    StorageKey key = CreateStorageKey(Prefix_AccessControl, id);
                    StorageItem storage_Access = engine.Snapshot.Storages.GetAndChange(key);
                    storage_Access.Value = ConvertUserArrayToBytes(newVoter);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        private bool RegisterVote(Snapshot snapshot, CreateState createState)
        {
            StorageKey key = CreateStorageKey(Prefix_CreateVote, createState.GetId());
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = createState.ToByteArray()
            });
            return true;
        }

        private bool AddVote(Snapshot snapshot, VoteState voteState)
        {
            StorageKey key = CreateStorageKey(Prefix_Vote, GetVoteKey(snapshot, voteState.GetId().ToScriptHash()));
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = voteState.ToByteArray()
            });
            return true;
        }

        private byte[] GetVoteKey(Snapshot snapshot, UInt160 id)
        {
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            int count = GetVoteCount(snapshot, index_key);
            UInt160 Index_Number = snapshot.Storages.GetAndChange(index_key).Value.ToScriptHash();
            return CreateState.ConcatByte(id.ToArray(),Index_Number.ToArray());
        }

        private int GetVoteCount(Snapshot snapshot, StorageKey index_key)
        {
            return snapshot.Storages.Find(index_key.Key).Count();
        }

        static byte[] ConvertUserArrayToBytes(List<UInt160> users)
        {
            if (users == null) return new byte[0];
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(memoryStream))
            {
                foreach (var user in users)
                {
                    bw.Write(user);
                }
                return memoryStream.ToArray();
            }
        }

        static List<UInt160> ConvertBytesToUserArray(byte[] data)
        {
            if (data == null) return null;
            List<UInt160> result = new List<UInt160>();

            using (var br = new BinaryReader(new MemoryStream(data)))
            {
                var Count = data.Length / 20;
                for (int i = 0; i < Count; i++)
                {
                    result.Add(br.ReadBytes(20).ToScriptHash());
                }
                return result;
            }
        }
    }

    internal class CreateState
    {
        private byte[] Id;
        private UInt256 TransactionHash;
        private UInt160 CallingScriptHash;
        private readonly UInt160 Originator;
        private string Title;
        private string Description;
        private BigInteger[] VoteCandidate;
        private bool IsSequence;

        public CreateState() { }

        public CreateState(UInt256 transactionHash, UInt160 callingScriptHash, UInt160 originator, string title, string description, BigInteger[] candidate, bool IsSeq)
        {
            TransactionHash = transactionHash;
            CallingScriptHash = callingScriptHash;
            Originator = originator;
            Title = title;
            Description = description;
            VoteCandidate = candidate;
            IsSequence = IsSeq;

            Id = new Crypto().Hash160(ConcatByte(TransactionHash.ToArray(), CallingScriptHash.ToArray()));
        }

        public byte[] ToByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public static CreateState FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as CreateState;
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
        }

        public byte[] GetId()
        {
            return this.Id;
        }

        public static byte[] ConcatByte(byte[] byteSource, byte[] newData)
        {
            List<byte> result = new List<byte>(byteSource);
            result.AddRange(newData);
            return result.ToArray();
        }
    }

    internal class VoteState
    {
        private readonly byte[] Id;
        private readonly UInt160 Voter;
        private readonly ICandidate Records;

        public VoteState(byte[] id, UInt160 voter, ICandidate candidate)
        {
            Id = id;
            Voter = voter;
            Records = candidate;
        }

        public byte[] GetId() => this.Id;
        public UInt160 GetVoter() => this.Voter;
        public ICandidate GetCandidate() => this.Records;

        public byte[] ToByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public static VoteState FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as VoteState;
                }
                catch (Exception e)
                {
                    throw e;
                }
                
            }
        }
    }

    internal interface ICandidate
    {
        byte[] GetByteArray();
        bool SetByteArray(byte[] data);
    }

    internal class MultiCandidate : ICandidate
    {
        public MultiCandidate() => this.candidateList = new List<int>();
        public MultiCandidate(List<int> lists) => this.candidateList = lists;

        private List<int> candidateList;
        public byte[] GetByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this.candidateList);
                return memoryStream.ToArray();
            }
        }

        public bool SetByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    this.candidateList = binaryFormatter.Deserialize(memoryStream) as List<int>;
                    return true;
                }
                catch(Exception e)
                {
                    throw e;
                }

            }
        }
    }
    internal class SingleCandidate : ICandidate
    {
        public SingleCandidate() => this.candidate = null;
        public SingleCandidate(int candidate) => this.candidate = candidate;

        private int? candidate;
        public byte[] GetByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this.candidate);
                return memoryStream.ToArray();
            }
        }

        public bool SetByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    this.candidate = binaryFormatter.Deserialize(memoryStream) as int?;
                    return true;
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
        }
    }

    internal class CalculatedVote
    {

    }

    internal class Statistic
    {
        UInt256 id;
        UInt160 originator;
        string title;
        string description;
        int[] candidate;
        int[][] resultMatrix;

        public byte[] ToByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public static Statistic FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as Statistic;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
