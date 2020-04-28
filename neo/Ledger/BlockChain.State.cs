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

        private RelayResultReason OnNewStateRoot(StateRoot state_root)
        {
            if (state_root.Index < StateRootEnableIndex || state_root.Index <= StateHeight) return RelayResultReason.Invalid;
            if (state_root.Witness is null) return RelayResultReason.Invalid;
            if (stateRootCache.ContainsKey(state_root.Index)) return RelayResultReason.AlreadyExists;
            if (state_root.Index > Height || (state_root.Index > StateHeight + 1 && state_root.Index != StateRootEnableIndex))
            {
                stateRootCache.Add(state_root.Index, state_root);
                return RelayResultReason.Succeed;
            }
            var state_root_to_verify = state_root;
            var state_roots_to_verify = new List<StateRoot>();
            while (true)
            {
                state_roots_to_verify.Add(state_root_to_verify);
                var index = state_root_to_verify.Index + 1;
                if (index > Height) break;
                if (!stateRootCache.TryGetValue(index, out state_root_to_verify)) break;
            }
            foreach (var state_root_verifying in state_roots_to_verify)
            {
                using (Snapshot snapshot = GetSnapshot())
                {
                    stateRootCache.Remove(state_root_verifying.Index);
                    if (!state_root_verifying.Verify(snapshot))
                    {
                        break;
                    }
                    var local_state = snapshot.StateRoots.GetAndChange(state_root_verifying.Index);
                    if (local_state.StateRoot.Root == state_root_verifying.Root && local_state.StateRoot.PreHash == state_root_verifying.PreHash)
                    {
                        snapshot.StateRootHashIndex.GetAndChange().Index = state_root_verifying.Index;
                        snapshot.StateRootHashIndex.GetAndChange().Hash = state_root_verifying.Hash;
                        local_state.StateRoot = state_root_verifying;
                        local_state.Flag = StateRootVerifyFlag.Verified;
                        if (state_root_verifying.Index + 3 > HeaderHeight)
                        {
                            system.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = state_root_verifying });
                        }
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
                        snapshot.StateRootHashIndex.GetAndChange().Index = root.Index;
                        snapshot.StateRootHashIndex.GetAndChange().Hash = root.Hash;
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