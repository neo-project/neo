using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public UInt160 AssetId;
        public string AssetName;
        public byte Decimals;

        public AssetDescriptor(StoreView snapshot, UInt160 asset_id)
        {
            var contract = snapshot.Contracts.TryGet(asset_id);
            if (contract == null) throw new ArgumentException();

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(asset_id, "decimals");
                script = sb.ToArray();
            }
            using ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, gas: 3_000_000);
            if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();
            this.AssetId = asset_id;
            this.AssetName = contract.Manifest.Name;
            this.Decimals = (byte)engine.ResultStack.Pop().GetInteger();
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
