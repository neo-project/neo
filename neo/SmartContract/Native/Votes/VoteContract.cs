#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native.Tokens;
using Neo.SmartContract.Native.Votes.Model;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Votes
{
    public sealed class VoteContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.VoteContract";

        private const byte Prefix_CreateVote = 10;
        private const byte Prefix_Vote = 11;
        private const byte Prefix_AccessControl = 12;
        private const byte Prefix_Result = 13;

        private StackItem CreateMultiVote(ApplicationEngine engine, VMArray args)
        {
            UInt160 originator = new UInt160(args[0].GetByteArray());
            if (!InteropService.CheckWitness(engine, originator)) return false;
            var tx = engine.ScriptContainer as Transaction;
            VoteCreateState createState = new VoteCreateState
                (tx.Hash,
                engine.CallingScriptHash,
                originator,
                args[1].GetString(),
                args[2].GetString(),
                (int)args[3].GetBigInteger(),
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
            VoteCreateState createState = new VoteCreateState
                (tx.Hash,
                engine.CallingScriptHash,
                originator,
                args[1].GetString(),
                args[2].GetString(),
                (int)args[3].GetBigInteger(),
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
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

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

            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            int voteLength = 0;
            if (!(info_state is null))
            {
                voteLength = (int)VoteCreateState.FromByteArray(info_state.Value).CandidateNumber;
            }

            MultiCandidate candidate = new MultiCandidate();
            List<int> state = candidate.GetCandidate();
            if (candidate.SetByteArray(args[2].GetByteArray()) && voteLength == state.Count() && state.Max() < voteLength)
            {
                VoteState voteState = new VoteState(voter, candidate);
                if (AddVote(engine.Snapshot, voteState, id))
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
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

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

            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            int voteLength = 0;
            if (!(info_state is null))
            {
                voteLength = (int)VoteCreateState.FromByteArray(info_state.Value).CandidateNumber;
            }

            SingleCandidate candidate = new SingleCandidate();
            if (candidate.SetByteArray(args[2].GetByteArray()) && candidate.GetCandidate() <= voteLength)
            {
                VoteState voteState = new VoteState(voter, candidate);
                if (AddVote(engine.Snapshot, voteState, id))
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
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));
            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_state = engine.Snapshot.Storages.TryGet(create_key);
            if (create_state is null) return null;
            return create_state.Value;
        }
        private StackItem GetMultiStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_byte = engine.Snapshot.Storages.TryGet(create_key);
            VoteCreateState create_state = VoteCreateState.FromByteArray(create_byte.Value);
            if (!create_state.IsSequence) return false;
            if (!InteropService.CheckWitness(engine, create_state.Originator)) return false;

            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            IEnumerable<KeyValuePair<StorageKey, StorageItem>> pairs = engine.Snapshot.Storages.Find(index_key.Key);

            if (pairs.Count() == 0) return false;

            MultiStatistic result = new MultiStatistic();
            foreach (KeyValuePair<StorageKey, StorageItem> pair in pairs)
            {
                VoteState vote_state = VoteState.FromByteArray(pair.Value.Value);
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, vote_state.GetVoter());
                MultiCandidate candidate = new MultiCandidate();
                if (candidate.SetByteArray(vote_state.ToByteArray()))
                {
                    result.AddVote(new CalculatedMultiVote
                    {
                        balance = account_balance,
                        vote = candidate.GetCandidate()
                    });
                }
                else
                {
                    //TODO: error handle
                }
            }
            //TODO: calculate result
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return result.ToByteArray();
        }
        private StackItem GetSingleStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_byte = engine.Snapshot.Storages.TryGet(create_key);
            VoteCreateState create_state = VoteCreateState.FromByteArray(create_byte.Value);
            if (create_state.IsSequence) return false;
            if (!InteropService.CheckWitness(engine, create_state.Originator)) return false;

            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            IEnumerable<KeyValuePair<StorageKey, StorageItem>> pairs = engine.Snapshot.Storages.Find(index_key.Key);

            if (pairs.Count() == 0) return false;

            SingleStatistic result = new SingleStatistic();
            foreach (KeyValuePair<StorageKey, StorageItem> pair in pairs)
            {
                VoteState vote_state = VoteState.FromByteArray(pair.Value.Value);
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, vote_state.GetVoter());
                SingleCandidate candidate = new SingleCandidate();
                if (candidate.SetByteArray(vote_state.ToByteArray()))
                {
                    result.AddVote(new CalculatedSingleVote
                    {
                        balance = account_balance,
                        vote = candidate.GetCandidate()
                    });
                }
                else
                {
                    //TODO; error handle
                }
            }
            //TODO: calculate result
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return result.ToByteArray();
        }
        private StackItem AccessControl(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null || args[1] == null || args[2] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

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
        private bool RegisterVote(Snapshot snapshot, VoteCreateState createState)
        {
            StorageKey key = CreateStorageKey(Prefix_CreateVote, createState.GetId());
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = createState.ToByteArray()
            });
            return true;
        }
        private bool AddVote(Snapshot snapshot, VoteState voteState, byte[] id)
        {
            StorageKey key = CreateStorageKey(Prefix_Vote, GetVoteKey(snapshot, id));
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = voteState.ToByteArray()
            });
            return true;
        }
        private bool AddResult(Snapshot snapshot, byte[] Result, byte[] id)
        {
            StorageKey key = CreateStorageKey(Prefix_Result, id);
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = Result
            });
            return true;
        }
        private byte[] GetVoteKey(Snapshot snapshot, byte[] id)
        {
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id);
            int count = GetVoteCount(snapshot, index_key);
            UInt160 Index_Number = snapshot.Storages.GetAndChange(index_key).Value.ToScriptHash();
            return VoteCreateState.ConcatByte(id.ToArray(), Index_Number.ToArray());
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
                catch (Exception e)
                {
                    throw e;
                }

            }
        }

        public List<int> GetCandidate()
        {
            return candidateList;
        }
    }
    internal class SingleCandidate : ICandidate
    {
        public SingleCandidate() { }
        public SingleCandidate(int candidate) => this.candidate = candidate;

        private int candidate;
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
                    this.candidate = (int)binaryFormatter.Deserialize(memoryStream);
                    return true;
                }
                catch
                {
                    return false;
                }

            }
        }

        public int GetCandidate()
        {
            return candidate;
        }
    }

    internal class CalculatedMultiVote
    {
        public int balance;
        public List<int> vote;
    }
    internal class MultiStatistic
    {
        List<CalculatedMultiVote> Matrix;
        int[][] resultMatrix;

        public void AddVote(CalculatedMultiVote vote)
        {
            Matrix.Add(vote);
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
        public static MultiStatistic FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as MultiStatistic;
                }
                catch
                {
                    return null;
                }
            }
        }

        public byte[] ShowResult()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this.resultMatrix);
                return memoryStream.ToArray();
            }
        }
        public bool CalculateResult(IMultiVoteModel Model)
        {
            try
            {
                resultMatrix = Model.GetResult(this.Matrix);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    internal class CalculatedSingleVote
    {
        public int balance;
        public int vote;
    }
    internal class SingleStatistic
    {
        List<CalculatedSingleVote> Matrix;
        int[] result;

        public void AddVote(CalculatedSingleVote vote)
        {
            Matrix.Add(vote);
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
        public static SingleStatistic FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as SingleStatistic;
                }
                catch
                {
                    return null;
                }
            }

        }
        public byte[] ShowResult()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this.result);
                return memoryStream.ToArray();
            }
        }
        public bool CalculateResult(ISingleVoteModel Model)
        {
            try
            {
                result = Model.GetResult(this.Matrix);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    interface IMultiVoteModel
    {
        //TODO: details for interface
        int[][] GetResult(List<CalculatedMultiVote> votes);
    }

    interface ISingleVoteModel
    {
        int[] GetResult(List<CalculatedSingleVote> votes);
    }
}
