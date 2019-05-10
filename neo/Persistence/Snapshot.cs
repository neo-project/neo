using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;

namespace Neo.Persistence
{
    public abstract class Snapshot : IDisposable, IPersistence
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, BlockState> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
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
            Blocks.Commit();
            Transactions.Commit();
            Contracts.Commit();
            Storages.Commit();
            HeaderHashList.Commit();
            NextValidators.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
        }

        public virtual void Dispose()
        {
        }

        public IEnumerable<(ECPoint PublicKey, BigInteger Votes)> GetRegisteredValidators()
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.NEO.ScriptHash, "getRegisteredValidators");
                script = sb.ToArray();
            }
            using (ApplicationEngine engine = ApplicationEngine.Run(script, this, testMode: true))
            {
                return ((VMArray)engine.ResultStack.Peek()).Select(p =>
                {
                    Struct @struct = (Struct)p;
                    return (@struct.GetByteArray().AsSerializable<ECPoint>(), @struct.GetBigInteger());
                });
            }
        }

        public ECPoint[] GetValidators()
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.NEO.ScriptHash, "getValidators");
                script = sb.ToArray();
            }
            using (ApplicationEngine engine = ApplicationEngine.Run(script, this, testMode: true))
            {
                return ((VMArray)engine.ResultStack.Peek()).Select(p => p.GetByteArray().AsSerializable<ECPoint>()).ToArray();
            }
        }
    }
}
