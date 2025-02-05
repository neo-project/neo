// Copyright (C) 2015-2025 The Neo Project.
//
// LogStorageStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Plugins.ApplicationLogs.Store
{
    public sealed class LogStorageStore : IDisposable
    {
        #region Prefixes

        private static readonly int Prefix_Size = sizeof(int) + sizeof(byte);
        private static readonly int Prefix_Block_Trigger_Size = Prefix_Size + UInt256.Length;
        private static readonly int Prefix_Execution_Block_Trigger_Size = Prefix_Size + UInt256.Length;

        private static readonly int Prefix_Id = 0x414c4f47;                 // Magic Code: (ALOG);
        private static readonly byte Prefix_Engine = 0x18;                  // Engine_GUID -> ScriptHash, Message
        private static readonly byte Prefix_Engine_Transaction = 0x19;      // TxHash -> Engine_GUID_List
        private static readonly byte Prefix_Block = 0x20;                   // BlockHash, Trigger -> NotifyLog_GUID_List
        private static readonly byte Prefix_Notify = 0x21;                  // NotifyLog_GUID -> ScriptHash, EventName, StackItem_GUID_List
        private static readonly byte Prefix_Contract = 0x22;                // ScriptHash, TimeStamp, EventIterIndex -> txHash, Trigger, NotifyLog_GUID
        private static readonly byte Prefix_Execution = 0x23;               // Execution_GUID -> Data, StackItem_GUID_List
        private static readonly byte Prefix_Execution_Block = 0x24;         // BlockHash, Trigger -> Execution_GUID
        private static readonly byte Prefix_Execution_Transaction = 0x25;   // TxHash -> Execution_GUID
        private static readonly byte Prefix_Transaction = 0x26;             // TxHash -> NotifyLog_GUID_List
        private static readonly byte Prefix_StackItem = 0xed;               // StackItem_GUID -> Data

        #endregion

        #region Global Variables

        private readonly ISnapshot _snapshot;

        #endregion

        #region Ctor

        public LogStorageStore(ISnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
            _snapshot = snapshot;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Put

        public Guid PutEngineState(EngineLogState state)
        {
            var id = Guid.NewGuid();
            var key = new KeyBuilder(Prefix_Id, Prefix_Engine)
                .Add(id.ToByteArray())
                .ToArray();
            _snapshot.Put(key, state.ToArray());
            return id;
        }

        public void PutTransactionEngineState(UInt256 hash, TransactionEngineLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Engine_Transaction)
                .Add(hash)
                .ToArray();
            _snapshot.Put(key, state.ToArray());
        }

        public void PutBlockState(UInt256 hash, TriggerType trigger, BlockLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Block)
                .Add(hash)
                .Add((byte)trigger)
                .ToArray();
            _snapshot.Put(key, state.ToArray());
        }

        public Guid PutNotifyState(NotifyLogState state)
        {
            var id = Guid.NewGuid();
            var key = new KeyBuilder(Prefix_Id, Prefix_Notify)
                .Add(id.ToByteArray())
                .ToArray();
            _snapshot.Put(key, state.ToArray());
            return id;
        }

        public void PutContractState(UInt160 scriptHash, ulong timestamp, uint iterIndex, ContractLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .AddBigEndian(timestamp)
                .AddBigEndian(iterIndex)
                .ToArray();
            _snapshot.Put(key, state.ToArray());
        }

        public Guid PutExecutionState(ExecutionLogState state)
        {
            var id = Guid.NewGuid();
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution)
                .Add(id.ToByteArray())
                .ToArray();
            _snapshot.Put(key, state.ToArray());
            return id;
        }

        public void PutExecutionBlockState(UInt256 blockHash, TriggerType trigger, Guid executionStateId)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution_Block)
                .Add(blockHash)
                .Add((byte)trigger)
                .ToArray();
            _snapshot.Put(key, executionStateId.ToByteArray());
        }

        public void PutExecutionTransactionState(UInt256 txHash, Guid executionStateId)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution_Transaction)
                .Add(txHash)
                .ToArray();
            _snapshot.Put(key, executionStateId.ToByteArray());
        }

        public void PutTransactionState(UInt256 hash, TransactionLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Transaction)
                .Add(hash)
                .ToArray();
            _snapshot.Put(key, state.ToArray());
        }

        public Guid PutStackItemState(StackItem stackItem)
        {
            var id = Guid.NewGuid();
            var key = new KeyBuilder(Prefix_Id, Prefix_StackItem)
                .Add(id.ToByteArray())
                .ToArray();
            try
            {
                _snapshot.Put(key, BinarySerializer.Serialize(stackItem, ExecutionEngineLimits.Default with { MaxItemSize = (uint)Settings.Default.MaxStackSize }));
            }
            catch
            {
                _snapshot.Put(key, BinarySerializer.Serialize(StackItem.Null, ExecutionEngineLimits.Default with { MaxItemSize = (uint)Settings.Default.MaxStackSize }));
            }
            return id;
        }

        #endregion

        #region Find

        public IEnumerable<(BlockLogState State, TriggerType Trigger)> FindBlockState(UInt256 hash)
        {
            var prefixKey = new KeyBuilder(Prefix_Id, Prefix_Block)
                .Add(hash)
                .ToArray();
            foreach (var (key, value) in _snapshot.Seek(prefixKey, SeekDirection.Forward))
            {
                if (key.AsSpan().StartsWith(prefixKey))
                    yield return (value.AsSerializable<BlockLogState>(), (TriggerType)key.AsSpan(Prefix_Block_Trigger_Size)[0]);
                else
                    yield break;
            }
        }

        public IEnumerable<ContractLogState> FindContractState(UInt160 scriptHash, uint page, uint pageSize)
        {
            var prefix = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .ToArray();
            var prefixKey = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .AddBigEndian(ulong.MaxValue) // Get newest to oldest (timestamp)
                .ToArray();
            uint index = 1;
            foreach (var (key, value) in _snapshot.Seek(prefixKey, SeekDirection.Backward)) // Get newest to oldest
            {
                if (key.AsSpan().StartsWith(prefix))
                {
                    if (index >= page && index < (pageSize + page))
                        yield return value.AsSerializable<ContractLogState>();
                    index++;
                }
                else
                    yield break;
            }
        }

        public IEnumerable<ContractLogState> FindContractState(UInt160 scriptHash, TriggerType trigger, uint page, uint pageSize)
        {
            var prefix = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .ToArray();
            var prefixKey = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .AddBigEndian(ulong.MaxValue) // Get newest to oldest (timestamp)
                .ToArray();
            uint index = 1;
            foreach (var (key, value) in _snapshot.Seek(prefixKey, SeekDirection.Backward)) // Get newest to oldest
            {
                if (key.AsSpan().StartsWith(prefix))
                {
                    var state = value.AsSerializable<ContractLogState>();
                    if (state.Trigger == trigger)
                    {
                        if (index >= page && index < (pageSize + page))
                            yield return state;
                        index++;
                    }
                }
                else
                    yield break;
            }
        }

        public IEnumerable<ContractLogState> FindContractState(UInt160 scriptHash, TriggerType trigger, string eventName, uint page, uint pageSize)
        {
            var prefix = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .ToArray();
            var prefixKey = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .AddBigEndian(ulong.MaxValue) // Get newest to oldest (timestamp)
                .ToArray();
            uint index = 1;
            foreach (var (key, value) in _snapshot.Seek(prefixKey, SeekDirection.Backward)) // Get newest to oldest
            {
                if (key.AsSpan().StartsWith(prefix))
                {
                    var state = value.AsSerializable<ContractLogState>();
                    if (state.Trigger == trigger && state.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (index >= page && index < (pageSize + page))
                            yield return state;
                        index++;
                    }
                }
                else
                    yield break;
            }
        }

        public IEnumerable<(Guid ExecutionStateId, TriggerType Trigger)> FindExecutionBlockState(UInt256 hash)
        {
            var prefixKey = new KeyBuilder(Prefix_Id, Prefix_Execution_Block)
                .Add(hash)
                .ToArray();
            foreach (var (key, value) in _snapshot.Seek(prefixKey, SeekDirection.Forward))
            {
                if (key.AsSpan().StartsWith(prefixKey))
                    yield return (new Guid(value), (TriggerType)key.AsSpan(Prefix_Execution_Block_Trigger_Size)[0]);
                else
                    yield break;
            }
        }

        #endregion

        #region TryGet

        public bool TryGetEngineState(Guid engineStateId, out EngineLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Engine)
                .Add(engineStateId.ToByteArray())
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<EngineLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetTransactionEngineState(UInt256 hash, out TransactionEngineLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Engine_Transaction)
                .Add(hash)
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<TransactionEngineLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetBlockState(UInt256 hash, TriggerType trigger, out BlockLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Block)
                .Add(hash)
                .Add((byte)trigger)
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<BlockLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetNotifyState(Guid notifyStateId, out NotifyLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Notify)
                .Add(notifyStateId.ToByteArray())
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<NotifyLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetContractState(UInt160 scriptHash, ulong timestamp, uint iterIndex, out ContractLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Contract)
                .Add(scriptHash)
                .AddBigEndian(timestamp)
                .AddBigEndian(iterIndex)
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<ContractLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetExecutionState(Guid executionStateId, out ExecutionLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution)
                .Add(executionStateId.ToByteArray())
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<ExecutionLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetExecutionBlockState(UInt256 blockHash, TriggerType trigger, out Guid executionStateId)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution_Block)
                .Add(blockHash)
                .Add((byte)trigger)
                .ToArray();
            executionStateId = _snapshot.TryGet(key, out var data) ? new Guid(data) : Guid.Empty;
            return data != null;
        }

        public bool TryGetExecutionTransactionState(UInt256 txHash, out Guid executionStateId)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Execution_Transaction)
                .Add(txHash)
                .ToArray();
            executionStateId = _snapshot.TryGet(key, out var data) ? new Guid(data) : Guid.Empty;
            return data != null;
        }

        public bool TryGetTransactionState(UInt256 hash, out TransactionLogState state)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_Transaction)
                .Add(hash)
                .ToArray();
            state = _snapshot.TryGet(key, out var data) ? data.AsSerializable<TransactionLogState>()! : null;
            return data != null && data.Length > 0;
        }

        public bool TryGetStackItemState(Guid stackItemId, out StackItem stackItem)
        {
            var key = new KeyBuilder(Prefix_Id, Prefix_StackItem)
                .Add(stackItemId.ToByteArray())
                .ToArray();
            stackItem = _snapshot.TryGet(key, out var data) ? BinarySerializer.Deserialize(data, ExecutionEngineLimits.Default) : StackItem.Null;
            return data != null;
        }

        #endregion
    }
}
