#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed partial class NeoToken : Nep5Token<NeoToken.NeoAccountState>
    {
        public override int Id => -1;
        public override string Name => "NEO";
        public override string Symbol => "neo";
        public override byte Decimals => 0;
        public BigInteger TotalAmount { get; }

        internal NeoToken()
        {
            this.TotalAmount = 100000000 * Factor;
        }

        public override BigInteger TotalSupply(StoreView snapshot)
        {
            return TotalAmount;
        }

        protected override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, NeoAccountState state, BigInteger amount)
        {
            DistributeGas(engine, account, state);
            if (amount.IsZero) return;
            if (state.VoteTo != null)
            {
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate, state.VoteTo.ToArray()));
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes += amount;

                if (state_validator.Votes == 0)
                {
                    UInt160 voteeAddr = Contract.CreateSignatureContract(state.VoteTo).ScriptHash;
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(CreateStorageKey(Prefix_VoterRewardPerCommittee, voteeAddr.ToArray()).ToArray()))
                        engine.Snapshot.Storages.Delete(key);
                }
            }
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            // Initialize economic parameters

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_GasPerBlock), new StorageItem
            {
                Value = (5 * GAS.Factor).ToByteArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_NeoHoldersRewardRatio), new StorageItem
            {
                Value = new byte[] { 10 }
            }); ;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_CommitteeRewardRatio), new StorageItem
            {
                Value = new byte[] { 5 }
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_VotersRewardRatio), new StorageItem
            {
                Value = new byte[] { 85 }
            });

            // Predistribution

            BigInteger amount = TotalAmount;
            for (int i = 0; i < Blockchain.StandbyCommittee.Length; i++)
            {
                ECPoint pubkey = Blockchain.StandbyCommittee[i];
                RegisterCandidateInternal(engine.Snapshot, pubkey);
                BigInteger balance = TotalAmount / 2 / (Blockchain.StandbyValidators.Length * 2 + (Blockchain.StandbyCommittee.Length - Blockchain.StandbyValidators.Length));
                if (i < Blockchain.StandbyValidators.Length) balance *= 2;
                UInt160 account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
                Mint(engine, account, balance);
                VoteInternal(engine.Snapshot, account, pubkey);
                amount -= balance;
            }
            Mint(engine, Blockchain.GetConsensusAddress(Blockchain.StandbyValidators), amount);
        }

        protected override void OnPersist(ApplicationEngine engine)
        {
            base.OnPersist(engine);

            // Save next validators

            var storages = engine.Snapshot.Storages;
            StorageItem nextValidatorItem = storages.GetAndChange(CreateStorageKey(Prefix_NextValidators), () => new StorageItem());
            nextValidatorItem.Value = GetValidators(engine.Snapshot).ToByteArray();

            // Mint & Record

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
            var holderRewardKey = CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue - index - RewardIndexOffset);
            var holderBorderKey = CreateStorageKey(Prefix_HolderRewardPerBlock, uint.MaxValue);
            var enumerator = storages.FindRange(holderRewardKey, holderBorderKey).GetEnumerator();
            if (enumerator.MoveNext())
                holderRewards += new BigInteger(enumerator.Current.Value.Value);
            storages.Add(holderRewardKey, new StorageItem() { Value = holderRewards.ToByteArray() });

            for (var i = 0; i < committeeVotes.Length; i++)
            {
                // Mint the reward for committee by each block

                UInt160 committeeAddr = Contract.CreateSignatureContract(committeeVotes[i].Item1).ScriptHash;
                GAS.Mint(engine, committeeAddr, committeeRewardPerBlock);

                // Keep track of incremental gains of committee voters

                BigInteger voterRewardPerCommittee = (i < validatorNumber ? 2 : 1) * voterRewardPerBlock / committeeVotes[i].Item2;
                enumerator = storages.Find(CreateStorageKey(Prefix_VoterRewardPerCommittee, committeeAddr).ToArray()).GetEnumerator();
                if (enumerator.MoveNext())
                    voterRewardPerCommittee += new BigInteger(enumerator.Current.Value.Value);
                var storageKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, committeeAddr, (uint.MaxValue - index - RewardIndexOffset));
                storages.Add(storageKey, new StorageItem() { Value = voterRewardPerCommittee.ToByteArray() });
            }
        }
    }
}
