using System.Collections.Generic;
using Akka.Actor;
using Neo.Cryptography.MPT;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public StateRoot LatestValidatorsStateRoot
        {
            get
            {
                var state_root = currentSnapshot.ValidatorsStateRoot.Get();
                return state_root.RootHash is null ? null : state_root;
            }
        }
        public long StateHeight => LatestValidatorsStateRoot is null ? -1 : (long)LatestValidatorsStateRoot.Index;

        private readonly int MaxStateRootCacheCount = 100;
        private Dictionary<uint, StateRoot> state_root_cache = new Dictionary<uint, StateRoot>();

        public UInt256 GetLocalStateRoot(uint index)
        {
            return currentSnapshot.LocalStateRoot.TryGet(index)?.Hash;
        }

        private VerifyResult OnNewStateRoot(StateRoot root)
        {
            if (LatestValidatorsStateRoot != null && root.Index <= LatestValidatorsStateRoot.Index) return VerifyResult.AlreadyExists;
            if (Height + MaxStateRootCacheCount < root.Index) return VerifyResult.Invalid;
            if (Height < root.Index && root.Index <= Height + MaxStateRootCacheCount)
            {
                if (!state_root_cache.TryAdd(root.Index, root)) return VerifyResult.AlreadyExists;
                return VerifyResult.Succeed;
            }
            var result = PersistCnStateRoot(root);
            if (result == VerifyResult.Succeed && Height < root.Index + 2)
                system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = root });
            return result;
        }

        private VerifyResult PersistCnStateRoot(StateRoot root)
        {
            if (!root.Verify(currentSnapshot)) return VerifyResult.Invalid;
            if (currentSnapshot.LocalStateRoot.TryGet(root.Index)?.Hash == root.RootHash)
            {
                using (SnapshotView snapshot = GetSnapshot())
                {
                    var confirmedRoot = snapshot.ValidatorsStateRoot.GetAndChange();
                    confirmedRoot.Version = root.Version;
                    confirmedRoot.Index = root.Index;
                    confirmedRoot.RootHash = root.RootHash;
                    confirmedRoot.Witness = root.Witness;
                    snapshot.Commit();
                }
                UpdateCurrentSnapshot();
                return VerifyResult.Succeed;
            }
            if (StateRoot.CurrentVersion < root.Version && Height < root.Index + 2)
                system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = root });
            return VerifyResult.Invalid;
        }

        private void CheckStateRootCache()
        {
            var index = Height - 1;
            if (0 <= index && state_root_cache.TryGetValue(index, out StateRoot root))
            {
                state_root_cache.Remove(index);
                OnNewStateRoot(root);
            }
        }
    }
}
