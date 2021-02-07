using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public UInt160 AssetId { get; }
        public string AssetName { get; }
        public string Symbol { get; }
        public byte Decimals { get; }

        public AssetDescriptor(DataCache snapshot, UInt160 asset_id)
        {
            var contract = NativeContract.ContractManagement.GetContract(snapshot, asset_id);
            if (contract is null) throw new ArgumentException();

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(asset_id, "decimals");
                sb.EmitDynamicCall(asset_id, "symbol");
                script = sb.ToArray();
            }
            using ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, gas: 0_10000000);
            if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();
            this.AssetId = asset_id;
            this.AssetName = contract.Manifest.Name;
            this.Symbol = engine.ResultStack.Pop().GetString();
            this.Decimals = (byte)engine.ResultStack.Pop().GetInteger();
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
