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

        public AssetDescriptor(UInt160 asset_id)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(asset_id, ContractParameterType.Integer, "decimals");
                sb.EmitAppCall(asset_id, ContractParameterType.String, "name");
                script = sb.ToArray();
            }
            using ApplicationEngine engine = ApplicationEngine.Run(script, extraGAS: 3_000_000);
            if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();
            this.AssetId = asset_id;
            this.AssetName = engine.ResultStack.Pop().GetString();
            this.Decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
