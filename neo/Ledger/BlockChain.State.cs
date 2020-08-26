using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.Trie.MPT;
using System;
using System.Collections.Generic;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public class ImportRoots { public IEnumerable<StateRoot> Roots; }
        public long StateHeight => currentSnapshot.StateHeight;
        private static uint StateRootEnableIndex => ProtocolSettings.Default.StateRootEnableIndex;
        private readonly Dictionary<uint, StateRoot> stateRootCache = new Dictionary<uint, StateRoot>();
        private readonly uint MaxRootCacheCount = 1000;

        public StateRootState GetStateRoot(UInt256 block_hash)
        {
            var block = GetBlock(block_hash);
            return block is null ? null : GetStateRoot(block.Index);
        }

        public StateRootState GetStateRoot(uint index)
        {
            return currentSnapshot.StateRoots.TryGet(index);
        }

        public bool GetStateProof(UInt256 root, StorageKey skey, out HashSet<byte[]> proof)
        {
            var trieReadOnlyDb = new TrieReadOnlyStore(Store, Prefixes.DATA_MPT);
            var readOnlyTrie = new MPTTrie(root, trieReadOnlyDb);
            return readOnlyTrie.GetProof(skey.ToArray(), out proof);
        }

        public bool VerifyProof(UInt256 root, byte[] key, HashSet<byte[]> proof, out byte[] value)
        {
            var result = MPTTrie.VerifyProof(root, key, proof, out value);
            if (result) value = value.AsSerializable<StorageItem>().Value;
            return result;
        }

        private RelayResultReason OnNewStateRoot(StateRoot stateRoot)
        {
            var expectedStateRootIndex = Math.Max(StateRootEnableIndex, StateHeight + 1);
            if (stateRoot.Index < expectedStateRootIndex) return RelayResultReason.Invalid;
            if (expectedStateRootIndex + MaxRootCacheCount < stateRoot.Index) return RelayResultReason.OutOfMemory;
            if (stateRootCache.ContainsKey(stateRoot.Index)) return RelayResultReason.AlreadyExists;
            if (stateRoot.Index > expectedStateRootIndex)
            {
                stateRootCache.Add(stateRoot.Index, stateRoot);
                return RelayResultReason.Succeed;
            }
            using (Snapshot snapshot = GetSnapshot())
            {
                while (stateRoot.Index <= Height)
                {
                    stateRootCache.Remove(stateRoot.Index);
                    if (!stateRoot.Verify(currentSnapshot)) break;
                    if (PersistCnStateRoot(stateRoot) == StateRootVerifyFlag.Invalid)
                        break;
                    if (stateRoot.Index + 3 > HeaderHeight)
                    {
                        system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = stateRoot });
                    }
                    if (!stateRootCache.TryGetValue(stateRoot.Index + 1, out stateRoot)) break;
                }
            }
            return RelayResultReason.Succeed;
        }

        private void OnStateRoots(StateRoot[] sts)
        {
            foreach (var state_root in sts)
            {
                var result = OnNewStateRoot(state_root);
                if (result != RelayResultReason.Succeed)
                    break;
            }
            system.TaskManager.Tell(new TaskManager.StateRootTaskCompleted(), Sender);
        }

        private void OnImportRoots(IEnumerable<StateRoot> roots)
        {
            foreach (StateRoot stateRoot in roots)
            {
                if (stateRoot.Index < Math.Max(StateHeight, StateRootEnableIndex)) continue;
                if (stateRoot.Index != Math.Max(StateHeight + 1, StateRootEnableIndex))
                    throw new InvalidOperationException();

                if (PersistCnStateRoot(stateRoot) == StateRootVerifyFlag.Invalid)
                    break;
            }
            Sender.Tell(new ImportCompleted());
        }

        private StateRootVerifyFlag PersistCnStateRoot(StateRoot stateRoot)
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                var localState = snapshot.StateRoots.GetAndChange(stateRoot.Index);
                if (localState.StateRoot.Root == stateRoot.Root && localState.StateRoot.PreHash == stateRoot.PreHash)
                {
                    RootHashIndex rootHashIndex = snapshot.StateRootHashIndex.GetAndChange();
                    rootHashIndex.Index = stateRoot.Index;
                    rootHashIndex.Hash = stateRoot.Hash;
                    localState.StateRoot = stateRoot;
                    localState.Flag = StateRootVerifyFlag.Verified;
                }
                else
                {
                    localState.Flag = StateRootVerifyFlag.Invalid;
                }
                snapshot.Commit();
                UpdateCurrentSnapshot();

                return localState.Flag;
            }
        }

        private void PersistLocalStateRoot()
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                var trieDb = new TrieReadOnlyStore(Store, Prefixes.DATA_MPT);
                var currentRoot = trieDb.GetRoot();
                var currentIndex = snapshot.Height;
                var preHash = UInt256.Zero;
                if (currentIndex > 0)
                {
                    var last_state_root = currentSnapshot.StateRoots.TryGet(currentIndex - 1);
                    preHash = last_state_root.StateRoot.Hash;
                }
                var stateRootState = new StateRootState
                {
                    Flag = StateRootVerifyFlag.Unverified,
                    StateRoot = new StateRoot
                    {
                        Version = MPTTrie.Version,
                        Index = currentIndex,
                        PreHash = preHash,
                        Root = currentRoot,
                    }
                };

                snapshot.StateRoots.Add(currentIndex, stateRootState);
                snapshot.Commit();
            }
        }

        private void CheckRootOnBlockPersistCompleted()
        {
            var index = (uint)Math.Max(StateHeight + 1, StateRootEnableIndex);
            if (GetStateRoot(index)?.Flag == StateRootVerifyFlag.Unverified && stateRootCache.TryGetValue(index, out StateRoot state_root))
            {
                stateRootCache.Remove(index);
                Self.Tell(state_root);
            }
        }
    }
}