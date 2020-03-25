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
    partial class InteropService
    {
        public static class Runtime
        {
            public const int MaxNotificationSize = 1024;

            public static readonly InteropDescriptor Platform = Register("System.Runtime.Platform", Runtime_Platform, 0_00000250, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetTrigger = Register("System.Runtime.GetTrigger", Runtime_GetTrigger, 0_00000250, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetTime = Register("System.Runtime.GetTime", Runtime_GetTime, 0_00000250, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetScriptContainer = Register("System.Runtime.GetScriptContainer", Runtime_GetScriptContainer, 0_00000250, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetExecutingScriptHash = Register("System.Runtime.GetExecutingScriptHash", Runtime_GetExecutingScriptHash, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetCallingScriptHash = Register("System.Runtime.GetCallingScriptHash", Runtime_GetCallingScriptHash, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetEntryScriptHash = Register("System.Runtime.GetEntryScriptHash", Runtime_GetEntryScriptHash, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor CheckWitness = Register("System.Runtime.CheckWitness", Runtime_CheckWitness, 0_00030000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetInvocationCounter = Register("System.Runtime.GetInvocationCounter", Runtime_GetInvocationCounter, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Log = Register("System.Runtime.Log", Runtime_Log, 0_01000000, TriggerType.All, CallFlags.AllowNotify);
            public static readonly InteropDescriptor Notify = Register("System.Runtime.Notify", Runtime_Notify, 0_01000000, TriggerType.All, CallFlags.AllowNotify);
            public static readonly InteropDescriptor GetNotifications = Register("System.Runtime.GetNotifications", Runtime_GetNotifications, 0_00010000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GasLeft = Register("System.Runtime.GasLeft", Runtime_GasLeft, 0_00000400, TriggerType.All, CallFlags.None);

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
                            size += primitive.GetByteLength();
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
                                    size += pair.Key.GetByteLength();
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

            internal static bool CheckWitnessInternal(ApplicationEngine engine, UInt160 hash)
            {
                if (engine.ScriptContainer is Transaction tx)
                {
                    Cosigner usage = tx.Cosigners.FirstOrDefault(p => p.Account.Equals(hash));
                    if (usage is null) return false;
                    if (usage.Scopes == WitnessScope.Global) return true;
                    if (usage.Scopes.HasFlag(WitnessScope.CalledByEntry))
                    {
                        if (engine.CallingScriptHash == engine.EntryScriptHash)
                            return true;
                    }
                    if (usage.Scopes.HasFlag(WitnessScope.CustomContracts))
                    {
                        if (usage.AllowedContracts.Contains(engine.CurrentScriptHash))
                            return true;
                    }
                    if (usage.Scopes.HasFlag(WitnessScope.CustomGroups))
                    {
                        var contract = engine.Snapshot.Contracts[engine.CallingScriptHash];
                        // check if current group is the required one
                        if (contract.Manifest.Groups.Select(p => p.PubKey).Intersect(usage.AllowedGroups).Any())
                            return true;
                    }
                    return false;
                }

                // only for non-Transaction types (Block, etc)

                var hashes_for_verifying = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot);
                return hashes_for_verifying.Contains(hash);
            }

            private static bool Runtime_Platform(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(Encoding.ASCII.GetBytes("NEO"));
                return true;
            }

            private static bool Runtime_GetTrigger(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push((int)engine.Trigger);
                return true;
            }

            private static bool Runtime_GetTime(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.PersistingBlock.Timestamp);
                return true;
            }

            private static bool Runtime_GetScriptContainer(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(
                    engine.ScriptContainer is IInteroperable value ? value.ToStackItem(engine.ReferenceCounter) :
                    StackItem.FromInterface(engine.ScriptContainer));
                return true;
            }

            private static bool Runtime_GetExecutingScriptHash(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.CurrentScriptHash.ToArray());
                return true;
            }

            private static bool Runtime_GetCallingScriptHash(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.CallingScriptHash?.ToArray() ?? StackItem.Null);
                return true;
            }

            private static bool Runtime_GetEntryScriptHash(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.EntryScriptHash.ToArray());
                return true;
            }

            private static bool Runtime_CheckWitness(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> hashOrPubkey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                UInt160 hash = hashOrPubkey.Length switch
                {
                    20 => new UInt160(hashOrPubkey),
                    33 => SmartContract.Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1)).ToScriptHash(),
                    _ => null
                };
                if (hash is null) return false;
                engine.CurrentContext.EvaluationStack.Push(CheckWitnessInternal(engine, hash));
                return true;
            }

            private static bool Runtime_GasLeft(ApplicationEngine engine)
            {
                engine.Push(engine.GasLeft);
                return true;
            }

            private static bool Runtime_GetInvocationCounter(ApplicationEngine engine)
            {
                if (!engine.InvocationCounter.TryGetValue(engine.CurrentScriptHash, out var counter))
                {
                    return false;
                }

                engine.CurrentContext.EvaluationStack.Push(counter);
                return true;
            }

            private static bool Runtime_Log(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> state = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                if (state.Length > MaxNotificationSize) return false;
                string message = Encoding.UTF8.GetString(state);
                engine.SendLog(engine.CurrentScriptHash, message);
                return true;
            }

            private static bool Runtime_Notify(ApplicationEngine engine)
            {
                StackItem state = engine.CurrentContext.EvaluationStack.Pop();
                if (!CheckItemForNotification(state)) return false;
                engine.SendNotification(engine.CurrentScriptHash, state);
                return true;
            }

            private static bool Runtime_GetNotifications(ApplicationEngine engine)
            {
                StackItem item = engine.CurrentContext.EvaluationStack.Pop();

                IEnumerable<NotifyEventArgs> notifications = engine.Notifications;
                if (!item.IsNull) // must filter by scriptHash
                {
                    var hash = new UInt160(item.GetSpan());
                    notifications = notifications.Where(p => p.ScriptHash == hash);
                }

                if (notifications.Count() > engine.MaxStackSize) return false;
                engine.Push(new Array(engine.ReferenceCounter, notifications.Select(u => new Array(engine.ReferenceCounter, new[] { u.ScriptHash.ToArray(), u.State }))));
                return true;
            }
        }
    }
}
