using Neo.Ledger;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class GasToken : Nep5Token<Nep5AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override int Decimals => 8;

        private const byte Prefix_TotalSupply = 11;

        private GasToken()
        {
        }

        protected override BigInteger TotalSupply(ApplicationEngine engine)
        {
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        internal void DistributeGas(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = CreateAccountKey(account);
            StorageItem storage = engine.Service.Snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Nep5AccountState().ToByteArray()
            });
            Nep5AccountState state = new Nep5AccountState(storage.Value);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), amount });
        }
    }
}
