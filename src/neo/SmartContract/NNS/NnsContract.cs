using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native.Tokens;
using System;
using System.Linq;

namespace Neo.SmartContract.NNS
{
    public partial class NnsContract : Nep11Token<DomainState, Nep11AccountState>
    {
        public override int Id => -5;
        public override string Name => "NNS";
        public override string Symbol => "nns";
        public override byte Decimals => 0;

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Admin), new StorageItem
            {
                Value = NEO.GetCommitteeMultiSigAddress(engine.Snapshot).ToArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_ReceiptAddress), new StorageItem
            {
                Value = NEO.GetCommitteeMultiSigAddress(engine.Snapshot).ToArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_RentalPrice), new StorageItem
            {
                Value = BitConverter.GetBytes(5_000_000_000L)
            });
            return true;
        }
    }
}
