using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public partial class NeoToken
    {
        private const byte Prefix_GasPerBlock = 17;
        private const byte Prefix_NeoHoldersRewardRatio = 73;
        private const byte Prefix_CommitteeRewardRatio = 19;
        private const byte Prefix_VotersRewardRatio = 67;

        private const byte Prefix_VoterRewardPerCommittee = 23;
        private const byte Prefix_HolderRewardPerBlock = 57;

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
        {
            if (gasPerBlock < 0) return false;
            if (!CheckCommitteeWitness(engine)) return false;
            StorageItem item = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_GasPerBlock));
            item.Value = gasPerBlock.ToByteArray();
            return true;
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetNeoHoldersRewardRatio(ApplicationEngine engine, int neoHoldersRewardRatio)
        {
            if (neoHoldersRewardRatio < 0 || neoHoldersRewardRatio > byte.MaxValue) return false;
            if (!CheckCommitteeWitness(engine)) return false;
            StorageItem item = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NeoHoldersRewardRatio));
            item.Value = new byte[] { (byte)neoHoldersRewardRatio };
            return true;
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetCommitteeRewardRatio(ApplicationEngine engine, int committeesRewardRatio)
        {
            if (committeesRewardRatio < 0 || committeesRewardRatio > byte.MaxValue) return false;
            if (!CheckCommitteeWitness(engine)) return false;
            StorageItem item = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_CommitteeRewardRatio));
            item.Value = new byte[] { (byte)committeesRewardRatio };
            return true;
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetVotersRewardRatio(ApplicationEngine engine, int votersRewardRatio)
        {
            if (votersRewardRatio < 0 || votersRewardRatio > byte.MaxValue) return false;
            if (!CheckCommitteeWitness(engine)) return false;
            StorageItem item = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_VotersRewardRatio));
            item.Value = new byte[] { (byte)votersRewardRatio };
            return true;
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public BigInteger GetGasPerBlock(StoreView snapshot)
        {
            return new BigInteger(snapshot.Storages.TryGet(CreateStorageKey(Prefix_GasPerBlock)).Value);
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public int GetNeoHoldersRewardRatio(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_NeoHoldersRewardRatio)).Value[0];
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public int GetCommitteeRewardRatio(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_CommitteeRewardRatio)).Value[0];
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public int GetVotersRewardRatio(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_VotersRewardRatio)).Value[0];
        }

        private int GetTotalRewardRatio(StoreView snapshot)
        {
            return GetVotersRewardRatio(snapshot) + GetCommitteeRewardRatio(snapshot) + GetNeoHoldersRewardRatio(snapshot);
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
        {
            BigInteger gas = CalculateBonus(engine.Snapshot, state.VoteTo, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Snapshot.PersistingBlock.Index;
            GAS.Mint(engine, account, gas);
        }

        private BigInteger CalculateBonus(StoreView snapshot, ECPoint votee, BigInteger value, uint start, uint end)
        {
            if (value.IsZero || start >= end) return BigInteger.Zero;
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));

            BigInteger neoHolderReward = CalculateNeoHolderBonus(snapshot, value, start, end);
            if (votee is null) return neoHolderReward;

            var voteAddr = Contract.CreateSignatureContract(votee).ScriptHash;
            var endKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, voteAddr, uint.MaxValue - start - 1);
            var startKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, voteAddr, uint.MaxValue - end - 1);
            var borderKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, voteAddr, uint.MaxValue);

            var enumerator = snapshot.Storages.FindRange(startKey, endKey).GetEnumerator();
            if (!enumerator.MoveNext()) return neoHolderReward;

            var endRewardPerNeo = new BigInteger(enumerator.Current.Value.Value);
            var startRewardPerNeo = BigInteger.Zero;

            enumerator = snapshot.Storages.FindRange(endKey, borderKey).GetEnumerator();
            if (enumerator.MoveNext())
                startRewardPerNeo = new BigInteger(enumerator.Current.Value.Value);

            return neoHolderReward + value * (endRewardPerNeo - startRewardPerNeo) / 10000L;
        }

        private BigInteger CalculateNeoHolderBonus(StoreView snapshot, BigInteger value, uint start, uint end)
        {
            var endRewardItem = snapshot.Storages.TryGet(CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue - end - 1));
            var startRewardItem = snapshot.Storages.TryGet(CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue - start - 1));
            BigInteger startReward = startRewardItem is null ? 0 : new BigInteger(startRewardItem.Value);
            return value * (new BigInteger(endRewardItem.Value) - startReward) / TotalAmount;
        }

        [ContractMethod(0_03000000, CallFlags.AllowStates)]
        public BigInteger UnclaimedGas(StoreView snapshot, UInt160 account, uint end)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateAccountKey(account));
            if (storage is null) return BigInteger.Zero;
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            return CalculateBonus(snapshot, state.VoteTo, state.Balance, state.BalanceHeight, end);
        }

        private void DistributeGasForCommittee(ApplicationEngine engine)
        {
            var gasPerBlock = GetGasPerBlock(engine.Snapshot);
            (ECPoint, BigInteger)[] committeeVotes = GetCommitteeVotes(engine.Snapshot);
            int validatorNumber = GetValidators(engine.Snapshot).Length;
            int totalRewardRatio = GetTotalRewardRatio(engine.Snapshot);
            BigInteger holderRewardPerBlock = gasPerBlock * GetNeoHoldersRewardRatio(engine.Snapshot) / totalRewardRatio;
            BigInteger committeeRewardPerBlock = gasPerBlock * GetCommitteeRewardRatio(engine.Snapshot) / totalRewardRatio / committeeVotes.Length;
            BigInteger voterRewardPerBlock = gasPerBlock * GetVotersRewardRatio(engine.Snapshot) / totalRewardRatio / (committeeVotes.Length + validatorNumber);

            // Keep track of incremental gains of neo holders

            var index = engine.Snapshot.PersistingBlock.Index;
            var holderRewards = holderRewardPerBlock;
            var holderRewardKey = CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue - index - 1);
            var holderBorderKey = CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue);
            var enumerator = engine.Snapshot.Storages.FindRange(holderRewardKey, holderBorderKey).GetEnumerator();
            if (enumerator.MoveNext())
                holderRewards += new BigInteger(enumerator.Current.Value.Value);
            engine.Snapshot.Storages.Add(holderRewardKey, new StorageItem() { Value = holderRewards.ToByteArray() });

            for (var i = 0; i < committeeVotes.Length; i++)
            {
                // Keep track of incremental gains of committee voters

                UInt160 committeeAddr = Contract.CreateSignatureContract(committeeVotes[i].Item1).ScriptHash;
                BigInteger voterRewardPerCommittee = (i < validatorNumber ? 2 : 1) * voterRewardPerBlock * 10000L / committeeVotes[i].Item2;
                enumerator = engine.Snapshot.Storages.Find(CreateStorageKey(Prefix_VoterRewardPerCommittee, committeeAddr).ToArray()).GetEnumerator();
                if (enumerator.MoveNext())
                    voterRewardPerCommittee += new BigInteger(enumerator.Current.Value.Value);
                var storageKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, committeeAddr, (uint.MaxValue - index - 1));
                engine.Snapshot.Storages.Add(storageKey, new StorageItem() { Value = voterRewardPerCommittee.ToByteArray() });

                // Mint the reward for committee by each block

                GAS.Mint(engine, committeeAddr, committeeRewardPerBlock);
            }
        }
    }
}
