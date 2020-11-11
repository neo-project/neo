using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public UInt160 AssetId;
        public string AssetName;
        public byte Decimals;

        public AssetDescriptor(UInt160 asset_id)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(asset_id, "decimals");
                sb.EmitAppCall(asset_id, "name");
                script = sb.ToArray();
            }
            using SnapshotView snapshot = Blockchain.Singleton.GetSnapshot();
            using ApplicationEngine engine = ApplicationEngine.Run(script, gas: 100_000 * NativeContract.Policy.GetFeeRatio(snapshot));
            if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();
            this.AssetId = asset_id;
            this.AssetName = engine.ResultStack.Pop().GetString();
            this.Decimals = (byte)engine.ResultStack.Pop().GetInteger();
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
