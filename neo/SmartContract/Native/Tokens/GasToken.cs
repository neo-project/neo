using System;
using System.Linq;
using System.Numerics;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class GasToken : Nep5Token<Nep5AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override int Decimals => 8;

        private const byte Prefix_SystemFeeAmount = 15;

        internal GasToken()
        {
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "distributeFees":
                    return DistributeFees(engine);
                case "getSysFeeAmount":
                    return GetSysFeeAmount(engine, (uint)args[0].GetBigInteger());
                default:
                    return base.Main(engine, operation, args);
            }
        }

        private bool DistributeFees(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.System) throw new InvalidOperationException();
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
                Burn(engine, tx.Sender, tx.Gas + tx.NetworkFee);
            ECPoint[] validators;
            if (engine.Snapshot.PersistingBlock.Index > 1)
                validators = engine.Snapshot.NextValidators.Get().Validators;
            else
                validators = Blockchain.StandbyValidators;
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.Snapshot.PersistingBlock.ConsensusData.PrimaryIndex]).ToScriptHash();
            Mint(engine, primary, engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.NetworkFee));
            BigInteger sys_fee = GetSysFeeAmount(engine, engine.Snapshot.PersistingBlock.Index - 1) + engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(engine.Snapshot.PersistingBlock.Index));
            engine.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = sys_fee.ToByteArray(),
                IsConstant = true
            });
            return true;
        }

        internal BigInteger GetSysFeeAmount(ApplicationEngine engine, uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(index));
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }
    }
}
