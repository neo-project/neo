using Neo.Core;
using System;

namespace Neo.Wallets
{
    public class TransferOutput
    {
        private object asset_id;
        private UInt256 asset_id_256;

        public object AssetId
        {
            get => asset_id;
            set
            {
                asset_id = value;
                asset_id_256 = value as UInt256;
            }
        }
        public BigDecimal Value;
        public UInt160 ScriptHash;

        public bool IsGlobalAsset => asset_id_256 != null;

        public TransactionOutput ToTxOutput()
        {
            if (asset_id_256 != null)
                return new TransactionOutput
                {
                    AssetId = asset_id_256,
                    Value = Value.ToFixed8(),
                    ScriptHash = ScriptHash
                };
            throw new NotSupportedException();
        }
    }
}
