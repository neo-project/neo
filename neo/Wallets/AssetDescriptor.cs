using Neo.Core;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public UIntBase AssetId;
        public string AssetName;
        public byte Decimals;

        public AssetDescriptor(UIntBase asset_id)
        {
            if (asset_id is UInt160 asset_id_160)
            {
                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(asset_id_160, "decimals");
                    sb.EmitAppCall(asset_id_160, "name");
                    script = sb.ToArray();
                }
                ApplicationEngine engine = ApplicationEngine.Run(script);
                if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();
                this.AssetId = asset_id;
                this.AssetName = engine.ResultStack.Pop().GetString();
                this.Decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
            }
            else
            {
                AssetState state = Blockchain.Default.GetAssetState((UInt256)asset_id);
                this.AssetId = state.AssetId;
                this.AssetName = state.GetName();
                this.Decimals = state.Precision;
            }
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
