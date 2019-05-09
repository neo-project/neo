using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.Persistence
{
    public abstract class Snapshot : IDisposable, IPersistence
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, BlockState> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<UInt160, AccountState> Accounts { get; }
        public abstract DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public abstract DataCache<ECPoint, ValidatorState> Validators { get; }
        public abstract DataCache<UInt256, AssetState> Assets { get; }
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<ValidatorsCountState> ValidatorsCount { get; }
        public abstract MetaDataCache<NextValidatorsState> NextValidators { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;

        public Snapshot Clone()
        {
            return new CloneSnapshot(this);
        }

        public virtual void Commit()
        {
            Accounts.DeleteWhere((k, v) => !v.IsFrozen && v.Votes.Length == 0 && v.Balances.All(p => p.Value <= Fixed8.Zero));
            UnspentCoins.DeleteWhere((k, v) => v.Items.All(p => p.HasFlag(CoinState.Spent)));
            Blocks.Commit();
            Transactions.Commit();
            Accounts.Commit();
            UnspentCoins.Commit();
            Validators.Commit();
            Assets.Commit();
            Contracts.Commit();
            Storages.Commit();
            HeaderHashList.Commit();
            ValidatorsCount.Commit();
            NextValidators.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
        }

        public virtual void Dispose()
        {
        }

        private ECPoint[] _validators = null;
        public ECPoint[] GetValidators()
        {
            if (_validators == null)
            {
                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(NativeContract.NEO.ScriptHash, "getValidators");
                    script = sb.ToArray();
                }
                using (ApplicationEngine engine = ApplicationEngine.Run(script, this, testMode: true))
                {
                    _validators = ((VMArray)engine.ResultStack.Peek()).Select(p => p.GetByteArray().AsSerializable<ECPoint>()).ToArray();
                }
            }
            return _validators;
        }
    }
}
