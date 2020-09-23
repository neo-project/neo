using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int MaxEventName = 32;
        public const int MaxNotificationSize = 1024;

        public static readonly InteropDescriptor System_Runtime_Platform = Register("System.Runtime.Platform", nameof(GetPlatform), 0_00000250, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetTrigger = Register("System.Runtime.GetTrigger", nameof(Trigger), 0_00000250, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetTime = Register("System.Runtime.GetTime", nameof(GetTime), 0_00000250, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Runtime_GetScriptContainer = Register("System.Runtime.GetScriptContainer", nameof(GetScriptContainer), 0_00000250, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetExecutingScriptHash = Register("System.Runtime.GetExecutingScriptHash", nameof(CurrentScriptHash), 0_00000400, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetCallingScriptHash = Register("System.Runtime.GetCallingScriptHash", nameof(CallingScriptHash), 0_00000400, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetEntryScriptHash = Register("System.Runtime.GetEntryScriptHash", nameof(EntryScriptHash), 0_00000400, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_CheckWitness = Register("System.Runtime.CheckWitness", nameof(CheckWitness), 0_00030000, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GetInvocationCounter = Register("System.Runtime.GetInvocationCounter", nameof(GetInvocationCounter), 0_00000400, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_Log = Register("System.Runtime.Log", nameof(RuntimeLog), 0_01000000, CallFlags.AllowNotify, false);
        public static readonly InteropDescriptor System_Runtime_Notify = Register("System.Runtime.Notify", nameof(RuntimeNotify), 0_01000000, CallFlags.AllowNotify, false);
        public static readonly InteropDescriptor System_Runtime_GetNotifications = Register("System.Runtime.GetNotifications", nameof(GetNotifications), 0_00010000, CallFlags.None, true);
        public static readonly InteropDescriptor System_Runtime_GasLeft = Register("System.Runtime.GasLeft", nameof(GasLeft), 0_00000400, CallFlags.None, true);

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

        protected internal string GetPlatform()
        {
            return "NEO";
        }

        protected internal ulong GetTime()
        {
            return Snapshot.PersistingBlock.Timestamp;
        }

        protected internal IInteroperable GetScriptContainer()
        {
            return ScriptContainer as IInteroperable;
        }

        protected internal bool CheckWitness(byte[] hashOrPubkey)
        {
            UInt160 hash = hashOrPubkey.Length switch
            {
                20 => new UInt160(hashOrPubkey),
                33 => Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1)).ToScriptHash(),
                _ => throw new ArgumentException()
            };
            return CheckWitnessInternal(hash);
        }

        protected internal bool CheckWitnessInternal(UInt160 hash)
        {
            if (hash.Equals(CallingScriptHash)) return true;

            if (ScriptContainer is Transaction tx)
            {
                Signer[] signers;
                OracleResponse response = tx.GetAttribute<OracleResponse>();
                if (response is null)
                {
                    signers = tx.Signers;
                }
                else
                {
                    OracleRequest request = NativeContract.Oracle.GetRequest(Snapshot, response.Id);
                    signers = Snapshot.GetTransaction(request.OriginalTxid).Signers;
                }
                Signer signer = signers.FirstOrDefault(p => p.Account.Equals(hash));
                if (signer is null) return false;
                if (signer.Scopes == WitnessScope.Global) return true;
                if (signer.Scopes.HasFlag(WitnessScope.CalledByEntry))
                {
                    if (CallingScriptHash == null || CallingScriptHash == EntryScriptHash)
                        return true;
                }
                if (signer.Scopes.HasFlag(WitnessScope.CustomContracts))
                {
                    if (signer.AllowedContracts.Contains(CurrentScriptHash))
                        return true;
                }
                if (signer.Scopes.HasFlag(WitnessScope.CustomGroups))
                {
                    // Check allow state callflag

                    if (!CurrentContext.GetState<ExecutionContextState>().CallFlags.HasFlag(CallFlags.AllowStates))
                        throw new InvalidOperationException($"Cannot call this SYSCALL without the flag AllowStates.");

                    var contract = Snapshot.Contracts[CallingScriptHash];
                    // check if current group is the required one
                    if (contract.Manifest.Groups.Select(p => p.PubKey).Intersect(signer.AllowedGroups).Any())
                        return true;
                }
                return false;
            }

            // Check allow state callflag

            if (!CurrentContext.GetState<ExecutionContextState>().CallFlags.HasFlag(CallFlags.AllowStates))
                throw new InvalidOperationException($"Cannot call this SYSCALL without the flag AllowStates.");

            // only for non-Transaction types (Block, etc)

            var hashes_for_verifying = ScriptContainer.GetScriptHashesForVerifying(Snapshot);
            return hashes_for_verifying.Contains(hash);
        }

        protected internal int GetInvocationCounter()
        {
            if (!invocationCounter.TryGetValue(CurrentScriptHash, out var counter))
                throw new InvalidOperationException();
            return counter;
        }

        protected internal void RuntimeLog(byte[] state)
        {
            if (state.Length > MaxNotificationSize) throw new ArgumentException();
            string message = Encoding.StrictUTF8.GetString(state);
            Log?.Invoke(this, new LogEventArgs(ScriptContainer, CurrentScriptHash, message));
        }

        protected internal void RuntimeNotify(byte[] eventName, Array state)
        {
            if (eventName.Length > MaxEventName) throw new ArgumentException();
            if (!CheckItemForNotification(state)) throw new ArgumentException();
            SendNotification(CurrentScriptHash, Encoding.StrictUTF8.GetString(eventName), state);
        }

        protected internal void SendNotification(UInt160 hash, string eventName, Array state)
        {
            NotifyEventArgs notification = new NotifyEventArgs(ScriptContainer, hash, eventName, (Array)state.DeepCopy());
            Notify?.Invoke(this, notification);
            notifications ??= new List<NotifyEventArgs>();
            notifications.Add(notification);
        }

        protected internal NotifyEventArgs[] GetNotifications(UInt160 hash)
        {
            IEnumerable<NotifyEventArgs> notifications = Notifications;
            if (hash != null) // must filter by scriptHash
                notifications = notifications.Where(p => p.ScriptHash == hash);
            NotifyEventArgs[] array = notifications.ToArray();
            if (array.Length > MaxStackSize) throw new InvalidOperationException();
            return array;
        }
    }
}
