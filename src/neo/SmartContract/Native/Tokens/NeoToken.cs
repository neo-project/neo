#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed partial class NeoToken : Nep5Token<NeoToken.AccountState>
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

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            if (base.TotalSupply(engine.Snapshot) != BigInteger.Zero) return false;

            BigInteger amount = TotalAmount;
            for (int i = 0; i < Blockchain.CommitteeMembersCount; i++)
            {
                ECPoint pubkey = Blockchain.StandbyCommittee[i];
                RegisterCandidate(engine.Snapshot, pubkey);
                BigInteger balance = TotalAmount / 2 / (Blockchain.ValidatorsCount * 2 + (Blockchain.CommitteeMembersCount - Blockchain.ValidatorsCount));
                if (i < Blockchain.ValidatorsCount) balance *= 2;
                UInt160 account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
                Mint(engine, account, balance);
                Vote(engine.Snapshot, account, pubkey);
                amount -= balance;
            }
            Mint(engine, Blockchain.GetConsensusAddress(Blockchain.StandbyValidators), amount);
            InitializeEconomicModel(engine);
            return true;
        }

        protected override bool OnPersist(ApplicationEngine engine)
        {
            if (!base.OnPersist(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NextValidators), () => new StorageItem());
            storage.Value = GetValidators(engine.Snapshot).ToByteArray();
            OnPersistEpochState(engine);
            return true;
        }

        protected override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, AccountState state, BigInteger amount)
        {
            DistributeGas(engine, account, state);
            if (amount.IsZero) return;
            if (state.VoteTo != null)
            {
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate, state.VoteTo.ToArray()));
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes += amount;
            }
        }
    }
}
