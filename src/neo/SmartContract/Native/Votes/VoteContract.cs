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
using Neo.SmartContract.Native.Votes.Interface;
using VMArray = Neo.VM.Types.Array;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    public sealed class VoteContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.VoteContract";
        private const byte Prefix_CreateVote = 100;
        private const byte Prefix_Vote = 101;
        private const byte Prefix_AccessControl = 102;
        private const byte Prefix_Result = 103;

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem CreateMultiVote(ApplicationEngine engine, VMArray args)
        {
            UInt160 originator = new UInt160(args[0].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, originator)) return false;
            var tx = engine.ScriptContainer as Transaction;
            var createState = new VoteCreateState()
            {
                TransactionHash = tx.Hash,
                CallingScriptHash = engine.CallingScriptHash ?? this.Hash,
                Originator = originator,
                Title = args[1].GetString(),
                Description = args[2].GetString(),
                CandidateNumber = (UInt32)args[3].GetBigInteger(),
                IsSequence = true
            };
            if (RegisterVote(engine.Snapshot, createState))
            {
                return tx.Hash.ToArray();
            }
            else
            {
                return false;
            }
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem CreateSingleVote(ApplicationEngine engine, VMArray args)
        {
            UInt160 originator = new UInt160(args[0].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, originator)) return false;
            var tx = engine.ScriptContainer as Transaction;
            var createState = new VoteCreateState()
            {
                TransactionHash = tx.Hash,
                CallingScriptHash = engine.CallingScriptHash ?? this.Hash,
                Originator = originator,
                Title = args[1].GetString(),
                Description = args[2].GetString(),
                CandidateNumber = (UInt32)args[3].GetBigInteger(),
                IsSequence = true
            };
            if (RegisterVote(engine.Snapshot, createState))
            {
                return tx.Hash.ToArray();
            }
            else
            {
                return false;
            }
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem MultiVote(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null || args[1] is null || args[2] is null) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            UInt160 voter = new UInt160(args[1].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, voter)) return false;
            StorageKey AccessKey = CreateStorageKey(Prefix_AccessControl, id);
            StorageItem access_state = engine.Snapshot.Storages.TryGet(AccessKey);
            if (!(access_state is null))
            {
                HashSet<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }
            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            MultiCandidate candidate = Neo.IO.Helper.AsSerializable<MultiCandidate>(args[2].GetSpan());
            VoteState voteState = new VoteState(voter, candidate);
            return (AddVote(engine.Snapshot, voteState, id));
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem SingleVote(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null || args[1] is null || args[2] is null) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            UInt160 voter = new UInt160(args[1].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, voter)) return false;
            StorageKey AccessKey = CreateStorageKey(Prefix_AccessControl, id);
            StorageItem access_state = engine.Snapshot.Storages.TryGet(AccessKey);
            if (!(access_state is null))
            {
                HashSet<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }
            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            UInt32 voteLength = 0;
            if (info_state is null) return false;
            VoteCreateState createState = Neo.IO.Helper.AsSerializable<VoteCreateState>(info_state.Value);
            voteLength = createState.CandidateNumber;
            SingleCandidate candidate = Neo.IO.Helper.AsSerializable<SingleCandidate>(args[2].GetSpan());
            if (candidate.GetCandidate() == 0 || candidate.GetCandidate() > voteLength) return false;
            VoteState voteState = new VoteState(voter, candidate);
            return (AddVote(engine.Snapshot, voteState, id));
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem GetVoteDetails(ApplicationEngine engine, VMArray args)
        {
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_state = engine.Snapshot.Storages.TryGet(create_key);
            return create_state.Value;
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray, SafeMethod = true)]
        private StackItem GetMultiStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            //Get Vote id
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_byte = engine.Snapshot.Storages.TryGet(create_key);
            VoteCreateState createState = Neo.IO.Helper.AsSerializable<VoteCreateState>(create_byte.Value);
            if (!createState.IsSequence || !InteropService.Runtime.CheckWitnessInternal(engine, createState.Originator)) return false;
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Hash, new[] { Prefix_Vote });
            IEnumerable<(StorageKey, StorageItem)> pairs = engine.Snapshot.Storages.Find(prefix_key).OrderByDescending(p => p.Key);
            if (pairs.Count() == 0) return false;
            MultiStatistic result = new MultiStatistic();
            List<UInt160> voteList = new List<UInt160>();
            foreach (var pair in pairs)
            {
                VoteState vote_state = Neo.IO.Helper.AsSerializable<VoteState>(pair.Item2.Value);
                UInt160 voter = vote_state.GetVoter();
                if (voteList.Contains(voter)) continue;
                voteList.Add(voter);
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, voter);
                MultiCandidate candidate = Neo.IO.Helper.AsSerializable<MultiCandidate>(vote_state.ToArray());
                result.AddVote(new CalculatedMultiVote(account_balance, candidate.GetCandidate()));
            }
            result.CalculateResult(new SchulzeModel());
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return resultMatrix;
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem GetSingleStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_byte = engine.Snapshot.Storages.TryGet(create_key);
            VoteCreateState createState = Neo.IO.Helper.AsSerializable<VoteCreateState>(create_byte.Value);
            if (createState.IsSequence) return false;
            if (!InteropService.Runtime.CheckWitnessInternal(engine, createState.Originator)) return false;
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Hash, index_key.Key);
            IEnumerable<(StorageKey, StorageItem)> pairs = engine.Snapshot.Storages.Find(prefix_key).OrderByDescending(p => p.Key);
            if (pairs.Count() == 0) return false;
            SingleStatistic result = new SingleStatistic();
            List<UInt160> voterList = new List<UInt160>();
            foreach (var pair in pairs)
            {
                VoteState vote_state = Neo.IO.Helper.AsSerializable<VoteState>(pair.Item2.Value);
                UInt160 voter = vote_state.GetVoter();
                if (voterList.Contains(voter)) continue;
                voterList.Add(vote_state.GetVoter());
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, vote_state.GetVoter());
                SingleCandidate candidate = Neo.IO.Helper.AsSerializable<SingleCandidate>(vote_state.GetCandidate().ToArray());
                result.AddVote(new CalculatedSingleVote(account_balance, candidate.GetCandidate()));
            }
            result.CalculateResult(new SingleModel());
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return resultMatrix;
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray)]
        private StackItem AccessControl(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null || args[1] is null || args[2] is null || args[1].GetSpan().Length % 20 != 0) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            HashSet<UInt160> newVoter = new HashSet<UInt160>();
            using (MemoryStream memoryStream = new MemoryStream(args[1].GetSpan().ToArray(), false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                int length = binaryReader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    newVoter.Add(new UInt160(binaryReader.ReadBytes(20)));
                }
            }
            bool IsAdd = args[2].ToBoolean();
            if (IsAdd)
            {
                StorageKey key = CreateStorageKey(Prefix_AccessControl, id);
                StorageItem storage_Access = engine.Snapshot.Storages.TryGet(key);
                if (storage_Access is null)
                {
                    return AddAccess(engine.Snapshot, key, newVoter);
                }
                else
                {
                    storage_Access = engine.Snapshot.Storages.GetAndChange(key);
                    HashSet<UInt160> oldVoter = ConvertBytesToUserArray(storage_Access.Value);
                    newVoter.UnionWith(oldVoter);
                    storage_Access.Value = ConvertUserArrayToBytes(newVoter);
                    return true;
                }
            }
            else
            {
                StorageKey key = CreateStorageKey(Prefix_AccessControl, id);
                StorageItem storage_Access = engine.Snapshot.Storages.TryGet(key);
                if (storage_Access is null)
                {
                    return AddAccess(engine.Snapshot, key, newVoter);
                }
                else
                {
                    storage_Access = engine.Snapshot.Storages.GetAndChange(key);
                    storage_Access.Value = ConvertUserArrayToBytes(newVoter);
                    return true;
                }
            }
        }

        [ContractMethod(0_01000000, ContractParameterType.ByteArray, SafeMethod = true)]
        private StackItem GetResult(ApplicationEngine engine, VMArray args)
        {
            if (args[0] is null) return false;
            UInt256 TxHash = new UInt256(args[0].GetSpan());
            var id = Crypto.Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), (engine.CallingScriptHash ?? this.Hash).ToArray()));
            StorageKey key = CreateStorageKey(Prefix_Result, id);
            StorageItem result = engine.Snapshot.Storages.TryGet(key);
            if (result is null) return false;

            return result.Value;
        }

        private bool RegisterVote(StoreView snapshot, VoteCreateState createState)
        {
            StorageKey key = CreateStorageKey(Prefix_CreateVote, createState.GetId());
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = createState.ToArray()
             });
            return true;
        }

        private bool AddAccess(StoreView snapshot, StorageKey key, HashSet<UInt160> whiteList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                foreach (UInt160 voter in whiteList)
                {
                    binaryWriter.Write(voter);
                }
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = memoryStream.ToArray()
                });
            }
            return true;
        }

        private bool AddVote(StoreView snapshot, VoteState voteState, byte[] id)
        {
            StorageKey key = CreateStorageKey(Prefix_Vote, GetVoteKey(snapshot, id));
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = voteState.ToArray()
            });
            return true;
        }

        private bool AddResult(StoreView snapshot, byte[] Result, byte[] id)
        {
            StorageKey key = CreateStorageKey(Prefix_Result, id);
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = Result
            });
            return true;
        }

        private byte[] GetVoteKey(StoreView snapshot, byte[] id)
        {
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id);
            int count = GetVoteCount(snapshot, index_key);
            using MemoryStream memoryStream = new MemoryStream();
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(count);
            return VoteCreateState.ConcatByte(id.ToArray(), memoryStream.ToArray());
        }

        public int GetVoteCount(StoreView snapshot, StorageKey index_key)
        {
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Hash, index_key.Key);
            return snapshot.Storages.Find(prefix_key).Count();
        }

        static byte[] ConvertUserArrayToBytes(HashSet<UInt160> users)
        {
            if (users is null) return new byte[0];
            using MemoryStream memoryStream = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(memoryStream);
            foreach (var user in users)
            {
                bw.Write(user);
            }
            return memoryStream.ToArray();
        }

        static HashSet<UInt160> ConvertBytesToUserArray(byte[] data)
        {
            if (data is null) return null;
            HashSet<UInt160> result = new HashSet<UInt160>();
            using var br = new BinaryReader(new MemoryStream(data));
            int length = br.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                result.Add(br.ReadBytes(20).ToScriptHash());
            }
            return result;
        }
    }

    internal class MultiStatistic
    {
        List<CalculatedMultiVote> Matrix = new List<CalculatedMultiVote>();
        int[,] resultMatrix;

        public void AddVote(CalculatedMultiVote vote)
        {
            Matrix.Add(vote);
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
            resultMatrix = Model.CalculateVote(this.Matrix);
            return true;
        }
    }

    internal class SingleStatistic
    {
        List<CalculatedSingleVote> Matrix = new List<CalculatedSingleVote>();
        int[] result;

        public void AddVote(CalculatedSingleVote vote)
        {
            Matrix.Add(vote);
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
            result = Model.CalculateVote(this.Matrix);
            return true;
        }
    }
}
