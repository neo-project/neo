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

namespace Neo.SmartContract.Native
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
                (UInt32)args[3].GetBigInteger(),
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
        [ContractMethod(0_01000000, ContractParameterType.ByteArray, SafeMethod = true)]
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
                (UInt32)args[3].GetBigInteger(),
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
                HashSet<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }

            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            UInt32 voteLength = 0;
            if (!(info_state is null))
            {
                using (MemoryStream memoryStream = new MemoryStream(info_state.Value, false))
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    VoteCreateState createState = new VoteCreateState();
                    createState.Deserialize(binaryReader);
                    voteLength = createState.CandidateNumber;
                }
            }

            MultiCandidate candidate = new MultiCandidate();
            using (MemoryStream memoryStream = new MemoryStream(args[2].GetByteArray(), false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                candidate.Deserialize(binaryReader);
                if (candidate.GetCandidate() == null || candidate.GetCandidate().Count > voteLength) return false;
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
                HashSet<UInt160> accessVoter = ConvertBytesToUserArray(access_state.Value);
                if (!accessVoter.Contains(voter))
                {
                    return false;
                }
            }

            StorageKey InfoKey = CreateStorageKey(Prefix_CreateVote, id);
            StorageItem info_state = engine.Snapshot.Storages.TryGet(InfoKey);
            UInt32 voteLength = 0;
            if (!(info_state is null))
            {
                using (MemoryStream memoryStream = new MemoryStream(info_state.Value, false))
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    VoteCreateState createState = new VoteCreateState();
                    createState.Deserialize(binaryReader);
                    voteLength = createState.CandidateNumber;
                }
            }

            SingleCandidate candidate = new SingleCandidate();
            using (MemoryStream memoryStream = new MemoryStream(args[2].GetByteArray(), false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                candidate.Deserialize(binaryReader);
                if (candidate.GetCandidate() == 0 || candidate.GetCandidate() > voteLength) return false;
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
        }
        [ContractMethod(0_01000000, ContractParameterType.ByteArray, SafeMethod = true)]
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
            using (MemoryStream memoryStream = new MemoryStream(create_byte.Value, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                VoteCreateState createState = new VoteCreateState();
                createState.Deserialize(binaryReader);
                if (!createState.IsSequence) return false;
                if (!InteropService.CheckWitness(engine, createState.Originator)) return false;
            }
            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            IEnumerable<KeyValuePair<StorageKey, StorageItem>> pairs = engine.Snapshot.Storages.Find(index_key.Key);

            if (pairs.Count() == 0) return false;

            MultiStatistic result = new MultiStatistic();
            foreach (KeyValuePair<StorageKey, StorageItem> pair in pairs)
            {
                VoteState vote_state = new VoteState();
                using (MemoryStream memoryStream = new MemoryStream(pair.Value.Value, false))
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    vote_state.Deserialize(binaryReader);
                }
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, vote_state.GetVoter());
                MultiCandidate candidate = new MultiCandidate();

                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))                
                {
                    vote_state.GetCandidate().Serialize(binaryWriter);
                    memoryStream.ToArray();
                    using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                    {
                        candidate.Deserialize(binaryReader);
                    }
                }
                result.AddVote(new CalculatedMultiVote(account_balance, candidate.GetCandidate()));
                        //TODO: error handle
            }
            result.CalculateResult(new SchulzeModel());
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return resultMatrix;
        }
        private StackItem GetSingleStatistic(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            StorageKey create_key = CreateStorageKey(Prefix_CreateVote, id.ToArray());
            StorageItem create_byte = engine.Snapshot.Storages.TryGet(create_key);
            using (MemoryStream memoryStream = new MemoryStream(create_byte.Value, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                VoteCreateState createState = new VoteCreateState();
                createState.Deserialize(binaryReader);
                if (!createState.IsSequence) return false;
                if (!InteropService.CheckWitness(engine, createState.Originator)) return false;
            }

            StorageKey index_key = CreateStorageKey(Prefix_Vote, id.ToArray());
            IEnumerable<KeyValuePair<StorageKey, StorageItem>> pairs = engine.Snapshot.Storages.Find(index_key.Key);

            if (pairs.Count() == 0) return false;

            SingleStatistic result = new SingleStatistic();
            foreach (KeyValuePair<StorageKey, StorageItem> pair in pairs)
            {
                VoteState vote_state = new VoteState();
                using (MemoryStream memoryStream = new MemoryStream(pair.Value.Value, false))
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    vote_state.Deserialize(binaryReader);
                }
                int account_balance = (int)new NeoToken().BalanceOf(engine.Snapshot, vote_state.GetVoter());
                SingleCandidate candidate = new SingleCandidate();

                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    vote_state.GetCandidate().Serialize(binaryWriter);
                    memoryStream.ToArray();
                    using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                    {
                        candidate.Deserialize(binaryReader);
                    }
                }
                result.AddVote(new CalculatedSingleVote(account_balance, candidate.GetCandidate()));
                //TODO: error handle
            }
            result.CalculateResult(new SingleModel());
            var resultMatrix = result.ShowResult();
            if (!AddResult(engine.Snapshot, resultMatrix, id))
            {
                return false;
            }
            return resultMatrix;
        }
        private StackItem AccessControl(ApplicationEngine engine, VMArray args)
        {
            if (args[0] == null || args[1] == null || args[2] == null) return false;
            UInt256 TxHash = new UInt256(args[0].GetByteArray());
            var id = new Crypto().Hash160(VoteCreateState.ConcatByte(TxHash.ToArray(), engine.CallingScriptHash.ToArray()));

            HashSet<UInt160> newVoter = new HashSet<UInt160>(args[1].GetByteArray().AsSerializableArray<UInt160>());

            bool IsAdd = args[2].GetBoolean();
            if (IsAdd)
            {
                try
                {
                    StorageKey key = CreateStorageKey(Prefix_AccessControl, id);
                    StorageItem storage_Access = engine.Snapshot.Storages.GetAndChange(key);
                    HashSet<UInt160> oldVoter = ConvertBytesToUserArray(storage_Access.Value);
                    newVoter.UnionWith(oldVoter);
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
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                createState.Serialize(binaryWriter);
                binaryWriter.Flush();
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = memoryStream.ToArray()
                });
            }
            return true;
        }
        private bool AddVote(Snapshot snapshot, VoteState voteState, byte[] id)
        {
            StorageKey key = CreateStorageKey(Prefix_Vote, GetVoteKey(snapshot, id));
            if (snapshot.Storages.TryGet(key) != null) return false;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                voteState.Serialize(binaryWriter);
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = memoryStream.ToArray()
                }) ;
                return true;
            }
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
        public int GetVoteCount(Snapshot snapshot, StorageKey index_key)
        {
            return snapshot.Storages.Find(index_key.Key).Count();
        }
        static byte[] ConvertUserArrayToBytes(HashSet<UInt160> users)
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
        static HashSet<UInt160> ConvertBytesToUserArray(byte[] data)
        {
            if (data == null) return null;
            HashSet<UInt160> result = new HashSet<UInt160>();
            using (var br = new BinaryReader(new MemoryStream(data)))
            {
                while (true)
                {
                    try
                    {
                        result.Add(br.ReadBytes(20).ToScriptHash());
                    }
                    catch
                    {
                        break;
                    }
                }
                return result;
            }
        }
    }
    internal class MultiStatistic
    {
        List<CalculatedMultiVote> Matrix;
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
            try
            {
                resultMatrix = Model.CalculateVote(this.Matrix);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class SingleStatistic
    {
        List<CalculatedSingleVote> Matrix;
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
            try
            {
                result = Model.CalculateVote(this.Matrix);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
