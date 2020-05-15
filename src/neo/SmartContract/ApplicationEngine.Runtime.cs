using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int MaxNotificationSize = 1024;

        private static bool CheckItemForNotification(StackItem state)
        {
            int size = 0;
            List<StackItem> items_checked = new List<StackItem>();
            Queue<StackItem> items_unchecked = new Queue<StackItem>();
            while (true)
            {
                switch (state)
                {
                    case Struct array:
                        foreach (StackItem item in array)
                            items_unchecked.Enqueue(item);
                        break;
                    case Array array:
                        if (items_checked.All(p => !ReferenceEquals(p, array)))
                        {
                            items_checked.Add(array);
                            foreach (StackItem item in array)
                                items_unchecked.Enqueue(item);
                        }
                        break;
                    case PrimitiveType primitive:
                        size += primitive.Size;
                        break;
                    case Null _:
                        break;
                    case InteropInterface _:
                        return false;
                    case Map map:
                        if (items_checked.All(p => !ReferenceEquals(p, map)))
                        {
                            items_checked.Add(map);
                            foreach (var pair in map)
                            {
                                size += pair.Key.Size;
                                items_unchecked.Enqueue(pair.Value);
                            }
                        }
                        break;
                }
                if (size > MaxNotificationSize) return false;
                if (items_unchecked.Count == 0) return true;
                state = items_unchecked.Dequeue();
            }
        }

        internal bool CheckWitnessInternal(UInt160 hash)
        {
            if (ScriptContainer is Transaction tx)
            {
                Cosigner cosigner = tx.Cosigners.FirstOrDefault(p => p.Account.Equals(hash));
                if (cosigner is null) return false;
                if (cosigner.Scopes == WitnessScope.Global) return true;
                if (cosigner.Scopes.HasFlag(WitnessScope.CalledByEntry))
                {
                    if (CallingScriptHash == EntryScriptHash)
                        return true;
                }
                if (cosigner.Scopes.HasFlag(WitnessScope.CustomContracts))
                {
                    if (cosigner.AllowedContracts.Contains(CurrentScriptHash))
                        return true;
                }
                if (cosigner.Scopes.HasFlag(WitnessScope.CustomGroups))
                {
                    var contract = Snapshot.Contracts[CallingScriptHash];
                    // check if current group is the required one
                    if (contract.Manifest.Groups.Select(p => p.PubKey).Intersect(cosigner.AllowedGroups).Any())
                        return true;
                }
                return false;
            }

            // only for non-Transaction types (Block, etc)

            var hashes_for_verifying = ScriptContainer.GetScriptHashesForVerifying(Snapshot);
            return hashes_for_verifying.Contains(hash);
        }

        [InteropService("System.Runtime.Platform", 0_00000250, TriggerType.All, CallFlags.None)]
        private bool Runtime_Platform()
        {
            Push("NEO");
            return true;
        }

        [InteropService("System.Runtime.GetTrigger", 0_00000250, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetTrigger()
        {
            Push((int)Trigger);
            return true;
        }

        [InteropService("System.Runtime.GetTime", 0_00000250, TriggerType.Application, CallFlags.AllowStates)]
        private bool Runtime_GetTime()
        {
            Push(Snapshot.PersistingBlock.Timestamp);
            return true;
        }

        [InteropService("System.Runtime.GetScriptContainer", 0_00000250, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetScriptContainer()
        {
            Push(ScriptContainer is IInteroperable value
                ? value.ToStackItem(ReferenceCounter)
                : StackItem.FromInterface(ScriptContainer));
            return true;
        }

        [InteropService("System.Runtime.GetExecutingScriptHash", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetExecutingScriptHash()
        {
            Push(CurrentScriptHash.ToArray());
            return true;
        }

        [InteropService("System.Runtime.GetCallingScriptHash", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetCallingScriptHash()
        {
            Push(CallingScriptHash?.ToArray() ?? StackItem.Null);
            return true;
        }

        [InteropService("System.Runtime.GetEntryScriptHash", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetEntryScriptHash()
        {
            Push(EntryScriptHash.ToArray());
            return true;
        }

        [InteropService("System.Runtime.CheckWitness", 0_00030000, TriggerType.All, CallFlags.AllowStates)]
        private bool Runtime_CheckWitness()
        {
            if (!TryPop(out ReadOnlySpan<byte> hashOrPubkey)) return false;
            UInt160 hash = hashOrPubkey.Length switch
            {
                20 => new UInt160(hashOrPubkey),
                33 => Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1)).ToScriptHash(),
                _ => null
            };
            if (hash is null) return false;
            Push(CheckWitnessInternal(hash));
            return true;
        }

        [InteropService("System.Runtime.GetInvocationCounter", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetInvocationCounter()
        {
            if (!invocationCounter.TryGetValue(CurrentScriptHash, out var counter))
                return false;
            Push(counter);
            return true;
        }

        [InteropService("System.Runtime.Log", 0_01000000, TriggerType.All, CallFlags.AllowNotify)]
        private bool Runtime_Log()
        {
            if (!TryPop(out ReadOnlySpan<byte> state)) return false;
            if (state.Length > MaxNotificationSize) return false;
            string message = Encoding.UTF8.GetString(state);
            Log?.Invoke(this, new LogEventArgs(ScriptContainer, CurrentScriptHash, message));
            return true;
        }

        [InteropService("System.Runtime.Notify", 0_01000000, TriggerType.All, CallFlags.AllowNotify)]
        private bool Runtime_Notify()
        {
            if (!CheckItemForNotification(Peek())) return false;
            SendNotification(CurrentScriptHash, Pop());
            return true;
        }

        [InteropService("System.Runtime.GetNotifications", 0_00010000, TriggerType.All, CallFlags.None)]
        private bool Runtime_GetNotifications()
        {
            StackItem item = Pop();

            IEnumerable<NotifyEventArgs> notifications = this.notifications;
            if (!item.IsNull) // must filter by scriptHash
            {
                var hash = new UInt160(item.GetSpan());
                notifications = notifications.Where(p => p.ScriptHash == hash);
            }

            if (notifications.Count() > MaxStackSize) return false;
            Push(new Array(ReferenceCounter, notifications.Select(u => new Array(ReferenceCounter, new[] { u.ScriptHash.ToArray(), u.State }))));
            return true;
        }

        [InteropService("System.Runtime.GasLeft", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Runtime_GasLeft()
        {
            Push(GasLeft);
            return true;
        }
    }
}
