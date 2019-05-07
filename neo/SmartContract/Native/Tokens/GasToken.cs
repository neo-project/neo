using Neo.Ledger;
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
    }
}
