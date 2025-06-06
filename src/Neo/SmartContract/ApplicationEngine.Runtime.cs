// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.Runtime.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// The maximum length of event name.
        /// </summary>
        public const int MaxEventName = 32;

        /// <summary>
        /// The maximum size of notification objects.
        /// </summary>
        public const int MaxNotificationSize = 1024;

        /// <summary>
        /// The maximum number of notifications per application execution.
        /// </summary>
        public const int MaxNotificationCount = 512;

        private uint random_times = 0;

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.Platform.
        /// Gets the name of the current platform.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_Platform = Register("System.Runtime.Platform", nameof(GetPlatform), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetNetwork.
        /// Gets the magic number of the current network.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetNetwork = Register("System.Runtime.GetNetwork", nameof(GetNetwork), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetAddressVersion.
        /// Gets the address version of the current network.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetAddressVersion = Register("System.Runtime.GetAddressVersion", nameof(GetAddressVersion), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetTrigger.
        /// Gets the trigger of the execution.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetTrigger = Register("System.Runtime.GetTrigger", nameof(Trigger), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetTime.
        /// Gets the timestamp of the current block.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetTime = Register("System.Runtime.GetTime", nameof(GetTime), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetScriptContainer.
        /// Gets the current script container.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetScriptContainer = Register("System.Runtime.GetScriptContainer", nameof(GetScriptContainer), 1 << 3, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetExecutingScriptHash.
        /// Gets the script hash of the current context.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetExecutingScriptHash = Register("System.Runtime.GetExecutingScriptHash", nameof(CurrentScriptHash), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetCallingScriptHash.
        /// Gets the script hash of the calling contract.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetCallingScriptHash = Register("System.Runtime.GetCallingScriptHash", nameof(CallingScriptHash), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetEntryScriptHash.
        /// Gets the script hash of the entry context.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetEntryScriptHash = Register("System.Runtime.GetEntryScriptHash", nameof(EntryScriptHash), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.LoadScript.
        /// Loads a script at rumtime.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_LoadScript = Register("System.Runtime.LoadScript", nameof(RuntimeLoadScript), 1 << 15, CallFlags.AllowCall);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.CheckWitness.
        /// Determines whether the specified account has witnessed the current transaction.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_CheckWitness = Register("System.Runtime.CheckWitness", nameof(CheckWitness), 1 << 10, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetInvocationCounter.
        /// Gets the number of times the current contract has been called during the execution.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetInvocationCounter = Register("System.Runtime.GetInvocationCounter", nameof(GetInvocationCounter), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetRandom.
        /// Gets the random number generated from the VRF.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetRandom = Register("System.Runtime.GetRandom", nameof(GetRandom), 0, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.Log.
        /// Writes a log.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_Log = Register("System.Runtime.Log", nameof(RuntimeLog), 1 << 15, CallFlags.AllowNotify);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.Notify.
        /// Sends a notification.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_Notify = Register("System.Runtime.Notify", nameof(RuntimeNotify), 1 << 15, CallFlags.AllowNotify);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GetNotifications.
        /// Gets the notifications sent by the specified contract during the execution.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GetNotifications = Register("System.Runtime.GetNotifications", nameof(GetNotifications), 1 << 12, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.GasLeft.
        /// Gets the remaining GAS that can be spent in order to complete the execution.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_GasLeft = Register("System.Runtime.GasLeft", nameof(GasLeft), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.BurnGas.
        /// Burning GAS to benefit the NEO ecosystem.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_BurnGas = Register("System.Runtime.BurnGas", nameof(BurnGas), 1 << 4, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Runtime.CurrentSigners.
        /// Get the Signers of the current transaction.
        /// </summary>
        public static readonly InteropDescriptor System_Runtime_CurrentSigners = Register("System.Runtime.CurrentSigners", nameof(GetCurrentSigners), 1 << 4, CallFlags.None);

        /// <summary>
        /// The implementation of System.Runtime.Platform.
        /// Gets the name of the current platform.
        /// </summary>
        /// <returns>It always returns "NEO".</returns>
        internal protected static string GetPlatform()
        {
            return "NEO";
        }

        /// <summary>
        /// The implementation of System.Runtime.GetNetwork.
        /// Gets the magic number of the current network.
        /// </summary>
        /// <returns>The magic number of the current network.</returns>
        internal protected uint GetNetwork()
        {
            return ProtocolSettings.Network;
        }

        /// <summary>
        /// The implementation of System.Runtime.GetAddressVersion.
        /// Gets the address version of the current network.
        /// </summary>
        /// <returns>The address version of the current network.</returns>
        internal protected byte GetAddressVersion()
        {
            return ProtocolSettings.AddressVersion;
        }

        /// <summary>
        /// The implementation of System.Runtime.GetTime.
        /// Gets the timestamp of the current block.
        /// </summary>
        /// <returns>The timestamp of the current block.</returns>
        protected internal ulong GetTime()
        {
            return PersistingBlock.Timestamp;
        }

        /// <summary>
        /// The implementation of System.Runtime.GetScriptContainer.
        /// Gets the current script container.
        /// </summary>
        /// <returns>The current script container.</returns>
        protected internal StackItem GetScriptContainer()
        {
            if (ScriptContainer is not IInteroperable interop) throw new InvalidOperationException();
            return interop.ToStackItem(ReferenceCounter);
        }

        /// <summary>
        /// The implementation of System.Runtime.LoadScript.
        /// Loads a script at rumtime.
        /// </summary>
        protected internal void RuntimeLoadScript(byte[] script, CallFlags callFlags, Array args)
        {
            if ((callFlags & ~CallFlags.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(callFlags));

            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            ExecutionContext context = LoadScript(new Script(script, true), configureState: p =>
            {
                p.CallingContext = CurrentContext;
                p.CallFlags = callFlags & state.CallFlags & CallFlags.ReadOnly;
                p.IsDynamicCall = true;
            });

            for (int i = args.Count - 1; i >= 0; i--)
                context.EvaluationStack.Push(args[i]);
        }

        /// <summary>
        /// The implementation of System.Runtime.CheckWitness.
        /// Determines whether the specified account has witnessed the current transaction.
        /// </summary>
        /// <param name="hashOrPubkey">The hash or public key of the account.</param>
        /// <returns><see langword="true"/> if the account has witnessed the current transaction; otherwise, <see langword="false"/>.</returns>
        protected internal bool CheckWitness(byte[] hashOrPubkey)
        {
            UInt160 hash = hashOrPubkey.Length switch
            {
                20 => new UInt160(hashOrPubkey),
                33 => Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1)).ToScriptHash(),
                _ => throw new ArgumentException("Invalid hashOrPubkey.", nameof(hashOrPubkey))
            };
            return CheckWitnessInternal(hash);
        }

        /// <summary>
        /// Determines whether the specified account has witnessed the current transaction.
        /// </summary>
        /// <param name="hash">The hash of the account.</param>
        /// <returns><see langword="true"/> if the account has witnessed the current transaction; otherwise, <see langword="false"/>.</returns>
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
                    OracleRequest request = NativeContract.Oracle.GetRequest(SnapshotCache, response.Id);
                    signers = NativeContract.Ledger.GetTransaction(SnapshotCache, request.OriginalTxid).Signers;
                }
                Signer signer = signers.FirstOrDefault(p => p.Account.Equals(hash));
                if (signer is null) return false;
                foreach (WitnessRule rule in signer.GetAllRules())
                {
                    if (rule.Condition.Match(this))
                        return rule.Action == WitnessRuleAction.Allow;
                }
                return false;
            }

            // If we don't have the ScriptContainer, we consider that there are no script hashes for verifying
            if (ScriptContainer is null) return false;

            // Check allow state callflag
            ValidateCallFlags(CallFlags.ReadStates);

            // only for non-Transaction types (Block, etc)
            return ScriptContainer.GetScriptHashesForVerifying(SnapshotCache).Contains(hash);
        }

        /// <summary>
        /// The implementation of System.Runtime.GetInvocationCounter.
        /// Gets the number of times the current contract has been called during the execution.
        /// </summary>
        /// <returns>The number of times the current contract has been called during the execution.</returns>
        protected internal int GetInvocationCounter()
        {
            if (!invocationCounter.TryGetValue(CurrentScriptHash, out var counter))
            {
                invocationCounter[CurrentScriptHash] = counter = 1;
            }
            return counter;
        }

        /// <summary>
        /// The implementation of System.Runtime.GetRandom.
        /// Gets the next random number.
        /// </summary>
        /// <returns>The next random number.</returns>
        protected internal BigInteger GetRandom()
        {
            byte[] buffer;
            // In the unit of datoshi, 1 datoshi = 1e-8 GAS
            long price;
            if (IsHardforkEnabled(Hardfork.HF_Aspidochelone))
            {
                buffer = Cryptography.Helper.Murmur128(nonceData, ProtocolSettings.Network + random_times++);
                price = 1 << 13;
            }
            else
            {
                buffer = nonceData = Cryptography.Helper.Murmur128(nonceData, ProtocolSettings.Network);
                price = 1 << 4;
            }
            AddFee(price * ExecFeeFactor);
            return new BigInteger(buffer, isUnsigned: true);
        }

        /// <summary>
        /// The implementation of System.Runtime.Log.
        /// Writes a log.
        /// </summary>
        /// <param name="state">The message of the log.</param>
        protected internal void RuntimeLog(byte[] state)
        {
            if (state.Length > MaxNotificationSize) throw new ArgumentException("Message is too long.", nameof(state));
            try
            {
                string message = state.ToStrictUtf8String();
                Log?.Invoke(this, new LogEventArgs(ScriptContainer, CurrentScriptHash, message));
            }
            catch
            {
                throw new ArgumentException("Failed to convert byte array to string: Invalid or non-printable UTF-8 sequence detected.", nameof(state));
            }
        }

        /// <summary>
        /// The implementation of System.Runtime.Notify.
        /// Sends a notification.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="state">The arguments of the event.</param>
        protected internal void RuntimeNotify(byte[] eventName, Array state)
        {
            if (!IsHardforkEnabled(Hardfork.HF_Basilisk))
            {
                RuntimeNotifyV1(eventName, state);
                return;
            }
            if (eventName.Length > MaxEventName) throw new ArgumentException(null, nameof(eventName));

            string name = eventName.ToStrictUtf8String();
            ContractState contract = CurrentContext.GetState<ExecutionContextState>().Contract;
            if (contract is null)
                throw new InvalidOperationException("Notifications are not allowed in dynamic scripts.");
            var @event = contract.Manifest.Abi.Events.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.Ordinal));
            if (@event is null)
                throw new InvalidOperationException($"Event `{name}` does not exist.");
            if (@event.Parameters.Length != state.Count)
                throw new InvalidOperationException("The number of the arguments does not match the formal parameters of the event.");
            for (int i = 0; i < @event.Parameters.Length; i++)
            {
                var p = @event.Parameters[i];
                if (!CheckItemType(state[i], p.Type))
                    throw new InvalidOperationException($"The type of the argument `{p.Name}` does not match the formal parameter.");
            }
            using MemoryStream ms = new(MaxNotificationSize);
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            BinarySerializer.Serialize(writer, state, MaxNotificationSize, Limits.MaxStackSize);
            SendNotification(CurrentScriptHash, name, state);
        }

        protected internal void RuntimeNotifyV1(byte[] eventName, Array state)
        {
            if (eventName.Length > MaxEventName) throw new ArgumentException(null, nameof(eventName));
            if (CurrentContext.GetState<ExecutionContextState>().Contract is null)
                throw new InvalidOperationException("Notifications are not allowed in dynamic scripts.");
            using MemoryStream ms = new(MaxNotificationSize);
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            BinarySerializer.Serialize(writer, state, MaxNotificationSize, Limits.MaxStackSize);
            SendNotification(CurrentScriptHash, eventName.ToStrictUtf8String(), state);
        }

        /// <summary>
        /// Sends a notification for the specified contract.
        /// </summary>
        /// <param name="hash">The hash of the specified contract.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="state">The arguments of the event.</param>
        protected internal void SendNotification(UInt160 hash, string eventName, Array state)
        {
            notifications ??= new List<NotifyEventArgs>();
            // Restrict the number of notifications for Application executions. Do not check
            // persisting triggers to avoid native persist failure. Do not check verification
            // trigger since verification context is loaded with ReadOnly flag.
            if (IsHardforkEnabled(Hardfork.HF_Echidna) && Trigger == TriggerType.Application && notifications.Count >= MaxNotificationCount)
            {
                throw new InvalidOperationException($"Maximum number of notifications `{MaxNotificationCount}` is reached.");
            }
            NotifyEventArgs notification = new(ScriptContainer, hash, eventName, (Array)state.DeepCopy(asImmutable: true));
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
            CurrentContext.GetState<ExecutionContextState>().NotificationCount++;
        }

        /// <summary>
        /// The implementation of System.Runtime.GetNotifications.
        /// Gets the notifications sent by the specified contract during the execution.
        /// </summary>
        /// <param name="hash">The hash of the specified contract. It can be set to <see langword="null"/> to get all notifications.</param>
        /// <returns>The notifications sent during the execution.</returns>
        protected internal Array GetNotifications(UInt160 hash)
        {
            IEnumerable<NotifyEventArgs> notifications = Notifications;
            if (hash != null) // must filter by scriptHash
                notifications = notifications.Where(p => p.ScriptHash == hash);
            var array = notifications.ToArray();
            if (array.Length > Limits.MaxStackSize) throw new InvalidOperationException();
            Array notifyArray = new(ReferenceCounter);
            foreach (var notify in array)
            {
                notifyArray.Add(notify.ToStackItem(ReferenceCounter, this));
            }
            return notifyArray;
        }

        /// <summary>
        /// The implementation of System.Runtime.BurnGas.
        /// Burning GAS to benefit the NEO ecosystem.
        /// </summary>
        /// <param name="datoshi">The amount of GAS to burn, in the unit of datoshi, 1 datoshi = 1e-8 GAS</param>
        protected internal void BurnGas(long datoshi)
        {
            if (datoshi <= 0)
                throw new InvalidOperationException("GAS must be positive.");
            AddFee(datoshi);
        }

        /// <summary>
        /// Get the Signers of the current transaction.
        /// </summary>
        /// <returns>The signers of the current transaction, or null if is not related to a transaction execution.</returns>
        protected internal Signer[] GetCurrentSigners()
        {
            if (ScriptContainer is Transaction tx)
                return tx.Signers;

            return null;
        }

        private static bool CheckItemType(StackItem item, ContractParameterType type)
        {
            StackItemType aType = item.Type;
            if (aType == StackItemType.Pointer) return false;
            switch (type)
            {
                case ContractParameterType.Any:
                    return true;
                case ContractParameterType.Boolean:
                    return aType == StackItemType.Boolean;
                case ContractParameterType.Integer:
                    return aType == StackItemType.Integer;
                case ContractParameterType.ByteArray:
                    return aType is StackItemType.Any or StackItemType.ByteString or StackItemType.Buffer;
                case ContractParameterType.String:
                    {
                        if (aType is StackItemType.ByteString or StackItemType.Buffer)
                        {
                            try
                            {
                                _ = item.GetSpan().ToStrictUtf8String(); // Prevent any non-UTF8 string
                                return true;
                            }
                            catch { }
                        }
                        return false;
                    }
                case ContractParameterType.Hash160:
                    if (aType == StackItemType.Any) return true;
                    if (aType != StackItemType.ByteString && aType != StackItemType.Buffer) return false;
                    return item.GetSpan().Length == UInt160.Length;
                case ContractParameterType.Hash256:
                    if (aType == StackItemType.Any) return true;
                    if (aType != StackItemType.ByteString && aType != StackItemType.Buffer) return false;
                    return item.GetSpan().Length == UInt256.Length;
                case ContractParameterType.PublicKey:
                    if (aType == StackItemType.Any) return true;
                    if (aType != StackItemType.ByteString && aType != StackItemType.Buffer) return false;
                    return item.GetSpan().Length == 33;
                case ContractParameterType.Signature:
                    if (aType == StackItemType.Any) return true;
                    if (aType != StackItemType.ByteString && aType != StackItemType.Buffer) return false;
                    return item.GetSpan().Length == 64;
                case ContractParameterType.Array:
                    return aType is StackItemType.Any or StackItemType.Array or StackItemType.Struct;
                case ContractParameterType.Map:
                    return aType is StackItemType.Any or StackItemType.Map;
                case ContractParameterType.InteropInterface:
                    return aType is StackItemType.Any or StackItemType.InteropInterface;
                default:
                    return false;
            }
        }
    }
}
