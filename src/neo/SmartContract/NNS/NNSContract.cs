using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    public partial class NNSContract: NativeContract
    {
        public override string ServiceName => "Neo.Native.NNS";
        public override int Id => -5;
        public string Name => "NNS";
        public string Symbol => "nns";
        public byte Decimals => 0;                                                                                                                                                                                                             

        protected const byte Prefix_TotalSupply = 22;
        protected const byte Prefix_Address = 23;

        [ContractMethod(0, ContractParameterType.String, Name = "name", SafeMethod = true)]
        protected StackItem NameMethod(ApplicationEngine engine, Array args)
        {
            return Name;
        }

        [ContractMethod(0, ContractParameterType.String, Name = "symbol", SafeMethod = true)]
        protected StackItem SymbolMethod(ApplicationEngine engine, Array args)
        {
            return Symbol;
        }

        [ContractMethod(0, ContractParameterType.Integer, Name = "decimals", SafeMethod = true)]
        protected StackItem DecimalsMethod(ApplicationEngine engine, Array args)
        {
            return (uint)Decimals;
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        protected StackItem TotalSupply(ApplicationEngine engine, Array args)
        {
            return TotalSupply(engine.Snapshot);
        }

        public virtual BigInteger TotalSupply(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "account" }, SafeMethod = true)]
        protected StackItem BalanceOf(ApplicationEngine engine, Array args)
        {
            return BalanceOf(engine.Snapshot, new UInt160(args[0].GetSpan()));
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account)
        {
            return BigInteger.Zero;
        }
    }
}
