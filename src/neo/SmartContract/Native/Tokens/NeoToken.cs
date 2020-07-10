#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
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
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate).Add(state.VoteTo));
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes += amount;

                if (state_validator.Votes == 0)
                {
                    UInt160 voteeAddr = Contract.CreateSignatureContract(state.VoteTo).ScriptHash;
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(voteeAddr).ToArray()))
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
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_RewardRatio), new StorageItem(new RewardRatio(){
                NeoHolder = 10,
                Committee = 5,
                Voter = 85
            }));

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

            var storages = engine.Snapshot.Storages;
            StorageItem nextValidatorItem = storages.GetAndChange(CreateStorageKey(Prefix_NextValidators), () => new StorageItem());
            nextValidatorItem.Value = GetValidators(engine.Snapshot).ToByteArray();

            DistributeGasForCommittee(engine);
        }
    }
}
