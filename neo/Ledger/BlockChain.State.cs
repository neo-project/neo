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
        public uint StateHeight => currentSnapshot.StateHeight;
        private uint StateRootEnableIndex => ProtocolSettings.Default.StateRootEnableIndex;
        private readonly Dictionary<uint, StateRoot> stateRootCache = new Dictionary<uint, StateRoot>();

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
            var readOnlyTrie = new MPTReadOnlyTrie(root, trieReadOnlyDb);
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
            if (stateRoot.Index < StateRootEnableIndex || stateRoot.Index <= StateHeight) return RelayResultReason.Invalid;
            if (!stateRoot.Verify(currentSnapshot)) return RelayResultReason.Invalid;
            if (stateRootCache.ContainsKey(stateRoot.Index)) return RelayResultReason.AlreadyExists;

            stateRootCache.Add(stateRoot.Index, stateRoot);
            if (stateRoot.Index > Height || (stateRoot.Index > StateHeight + 1 && stateRoot.Index != StateRootEnableIndex))
                return RelayResultReason.Succeed;

            using (Snapshot snapshot = GetSnapshot())
            {
                var index = stateRoot.Index;
                while (index <= Height)
                {
                    if (!stateRootCache.TryGetValue(index++, out StateRoot stateVerifying)) break;

                    var localState = snapshot.StateRoots.GetAndChange(stateVerifying.Index);
                    if (localState.StateRoot.Root == stateVerifying.Root && localState.StateRoot.PreHash == stateVerifying.PreHash)
                    {
                        HashIndexState hashIndexState = snapshot.StateRootHashIndex.GetAndChange();
                        hashIndexState.Index = stateVerifying.Index;
                        hashIndexState.Hash = stateVerifying.Hash;
                        localState.StateRoot = stateVerifying;
                        localState.Flag = StateRootVerifyFlag.Verified;
                        if (stateVerifying.Index + 3 > HeaderHeight)
                        {// TODO remove +3 and use LocalNode.RelayDirectly
                            system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = stateVerifying });
                        }
                    }
                    else
                    {
                        localState.Flag = StateRootVerifyFlag.Invalid;
                    }
                    snapshot.Commit();
                    UpdateCurrentSnapshot();
                    stateRootCache.Remove(stateVerifying.Index);

                    if (localState.Flag == StateRootVerifyFlag.Invalid) break;
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
                {
                    break;
                }
            }
            system.TaskManager.Tell(new TaskManager.StateRootTaskCompleted(), Sender);
        }

        private void OnImportRoots(IEnumerable<StateRoot> roots)
        {
            foreach (StateRoot root in roots)
            {
                if (root.Index < Math.Max(StateHeight, StateRootEnableIndex)) continue;
                if (root.Index != Math.Max(StateHeight + 1, StateRootEnableIndex))
                    throw new InvalidOperationException();
                using (Snapshot snapshot = GetSnapshot())
                {
                    var local_state = snapshot.StateRoots.GetAndChange(root.Index);
                    if (local_state.Flag == StateRootVerifyFlag.Invalid) break;
                    if (local_state.StateRoot.Root == root.Root && local_state.StateRoot.PreHash == root.PreHash)
                    {
                        HashIndexState hashIndexState = snapshot.StateRootHashIndex.GetAndChange();
                        hashIndexState.Index = root.Index;
                        hashIndexState.Hash = root.Hash;
                        local_state.StateRoot = root;
                        local_state.Flag = StateRootVerifyFlag.Verified;
                    }
                    else
                    {
                        local_state.Flag = StateRootVerifyFlag.Invalid;
                    }
                    snapshot.Commit();
                    UpdateCurrentSnapshot();
                    if (local_state.Flag == StateRootVerifyFlag.Invalid) break;
                }
            }
            Sender.Tell(new ImportCompleted());
        }

        private void PersistStateRoot()
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                var trie_db = new TrieReadOnlyStore(Store, Prefixes.DATA_MPT);
                var current_root = trie_db.GetRoot();
                var current_index = snapshot.Height;
                var pre_hash = UInt256.Zero;
                if (current_index > 0)
                {
                    var last_state_root = currentSnapshot.StateRoots.TryGet(current_index - 1);
                    pre_hash = last_state_root.StateRoot.Hash;
                }

                var state_root = new StateRoot
                {
                    Version = MPTTrie.Version,
                    Index = current_index,
                    PreHash = pre_hash,
                    Root = current_root,
                };
                var state_root_state = new StateRootState
                {
                    Flag = StateRootVerifyFlag.Unverified,
                    StateRoot = state_root,
                };
                snapshot.StateRoots.Add(current_index, state_root_state);
                snapshot.Commit();
            }
        }

        private void CheckRootOnBlockPersistCompleted()
        {
            var index = Math.Max(StateHeight + 1, StateRootEnableIndex);
            if (GetStateRoot(index)?.Flag == StateRootVerifyFlag.Unverified && stateRootCache.TryGetValue(index, out StateRoot state_root))
            {
                stateRootCache.Remove(index);
                Self.Tell(state_root);
            }
        }
    }
}