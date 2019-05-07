using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep5Token<T> : NativeContractBase
        where T : Nep5AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-5", "NEP-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract int Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_Account = 20;

        protected Nep5Token()
        {
            this.Factor = BigInteger.Pow(10, Decimals);
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, Array args)
        {
            switch (operation)
            {
                case "name":
                    return Name;
                case "symbol":
                    return Symbol;
                case "decimals":
                    return Decimals;
                case "totalSupply":
                    return TotalSupply(engine);
                case "balanceOf":
                    return BalanceOf(engine, new UInt160(args[0].GetByteArray()));
                case "transfer":
                    return Transfer(engine, new UInt160(args[0].GetByteArray()), new UInt160(args[1].GetByteArray()), args[2].GetBigInteger());
                default:
                    return base.Main(engine, operation, args);
            }
        }

        protected abstract BigInteger TotalSupply(ApplicationEngine engine);

        protected virtual BigInteger BalanceOf(ApplicationEngine engine, UInt160 account)
        {
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Account, account));
            if (storage is null) return BigInteger.Zero;
            Nep5AccountState state = new Nep5AccountState(storage.Value);
            return state.Balance;
        }

        protected abstract bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount);
    }
}
