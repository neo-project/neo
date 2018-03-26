using Neo.Core;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public object AssetId;
        public string AssetName;
        public byte Decimals;

        public AssetDescriptor(object asset_id)
        {
            UInt160 asset_id_160 = asset_id as UInt160;
            UInt256 asset_id_256 = asset_id as UInt256;
            if (asset_id_160 != null)
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
                this.AssetName = engine.EvaluationStack.Pop().GetString();
                this.Decimals = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            }
            else if (asset_id_256 != null)
            {
                AssetState state = Blockchain.Default.GetAssetState(asset_id_256);
                this.AssetId = asset_id;
                this.AssetName = state.GetName();
                this.Decimals = state.Precision;
            }
            else
                throw new NotSupportedException();
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
