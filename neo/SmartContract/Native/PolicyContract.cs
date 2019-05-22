using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Native
{
    public sealed class PolicyContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.Policy";

        private const byte Prefix_MaxTransactionsPerBlock = 23;
        private const byte Prefix_MaxLowPriorityTransactionsPerBlock = 34;
        private const byte Prefix_MaxLowPriorityTransactionSize = 29;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_BlockedAccounts = 15;

        public PolicyContract() : base()
        {
            Manifest.Features = ContractPropertyState.HasStorage;

            var list = new List<ContractMethodDescription>(Manifest.Abi.Methods)
            {
                new ContractMethodDescription()
                {
                    Name = "getMaxTransactionsPerBlock",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Integer
                },
                new ContractMethodDescription()
                {
                    Name = "getMaxLowPriorityTransactionsPerBlock",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Integer
                },
                new ContractMethodDescription()
                {
                    Name = "getMaxLowPriorityTransactionSize",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Integer
                },
                new ContractMethodDescription()
                {
                    Name = "getFeePerByte",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Integer
                },
                new ContractMethodDescription()
                {
                    Name = "getBlockedAccounts",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Integer
                },
                new ContractMethodDescription()
                {
                    Name = "setMaxTransactionsPerBlock",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "value",
                             Type = ContractParameterType.Integer
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                },
                new ContractMethodDescription()
                {
                    Name = "setMaxLowPriorityTransactionsPerBlock",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "value",
                             Type = ContractParameterType.Integer
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                },
                new ContractMethodDescription()
                {
                    Name = "setMaxLowPriorityTransactionSize",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "value",
                             Type = ContractParameterType.Integer
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                },
                new ContractMethodDescription()
                {
                    Name = "setFeePerByte",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "value",
                             Type = ContractParameterType.Integer
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                },

                new ContractMethodDescription()
                {
                    Name = "blockAccount",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "account",
                             Type = ContractParameterType.Hash160
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                },
                new ContractMethodDescription()
                {
                    Name = "unblockAccount",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                             Name = "account",
                             Type = ContractParameterType.Hash160
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                }
            };

            Manifest.Abi.Methods = list.ToArray();
        }

        private bool CheckValidators(ApplicationEngine engine)
        {
            UInt256 prev_hash = engine.Snapshot.PersistingBlock.PrevHash;
            TrimmedBlock prev_block = engine.Snapshot.Blocks[prev_hash];
            return InteropService.CheckWitness(engine, prev_block.NextConsensus);
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VM.Types.Array args)
        {
            switch (operation)
            {
                case "getMaxTransactionsPerBlock":
                    return GetMaxTransactionsPerBlock(engine.Snapshot);
                case "getMaxLowPriorityTransactionsPerBlock":
                    return GetMaxLowPriorityTransactionsPerBlock(engine.Snapshot);
                case "getMaxLowPriorityTransactionSize":
                    return GetMaxLowPriorityTransactionSize(engine.Snapshot);
                case "getFeePerByte":
                    return GetFeePerByte(engine.Snapshot);
                case "getBlockedAccounts":
                    return GetBlockedAccounts(engine.Snapshot).Select(p => (StackItem)p.ToArray()).ToArray();
                case "setMaxTransactionsPerBlock":
                    return SetMaxTransactionsPerBlock(engine, (uint)args[0].GetBigInteger());
                case "setMaxLowPriorityTransactionsPerBlock":
                    return SetMaxLowPriorityTransactionsPerBlock(engine, (uint)args[0].GetBigInteger());
                case "setMaxLowPriorityTransactionSize":
                    return SetMaxLowPriorityTransactionSize(engine, (uint)args[0].GetBigInteger());
                case "setFeePerByte":
                    return SetFeePerByte(engine, (long)args[0].GetBigInteger());
                case "blockAccount":
                    return BlockAccount(engine, new UInt160(args[0].GetByteArray()));
                case "unblockAccount":
                    return UnblockAccount(engine, new UInt160(args[0].GetByteArray()));
                default:
                    return base.Main(engine, operation, args);
            }
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxTransactionsPerBlock), new StorageItem
            {
                Value = BitConverter.GetBytes(512u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxLowPriorityTransactionsPerBlock), new StorageItem
            {
                Value = BitConverter.GetBytes(20u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MaxLowPriorityTransactionSize), new StorageItem
            {
                Value = BitConverter.GetBytes(256u)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_FeePerByte), new StorageItem
            {
                Value = BitConverter.GetBytes(1000L)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_BlockedAccounts), new StorageItem
            {
                Value = new UInt160[0].ToByteArray()
            });
            return true;
        }

        public uint GetMaxTransactionsPerBlock(Snapshot snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_MaxTransactionsPerBlock)].Value, 0);
        }

        public uint GetMaxLowPriorityTransactionsPerBlock(Snapshot snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_MaxLowPriorityTransactionsPerBlock)].Value, 0);
        }

        public uint GetMaxLowPriorityTransactionSize(Snapshot snapshot)
        {
            return BitConverter.ToUInt32(snapshot.Storages[CreateStorageKey(Prefix_MaxLowPriorityTransactionSize)].Value, 0);
        }

        public long GetFeePerByte(Snapshot snapshot)
        {
            return BitConverter.ToInt64(snapshot.Storages[CreateStorageKey(Prefix_FeePerByte)].Value, 0);
        }

        public UInt160[] GetBlockedAccounts(Snapshot snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_BlockedAccounts)].Value.AsSerializableArray<UInt160>();
        }

        private bool SetMaxTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxTransactionsPerBlock));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool SetMaxLowPriorityTransactionsPerBlock(ApplicationEngine engine, uint value)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxLowPriorityTransactionsPerBlock));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool SetMaxLowPriorityTransactionSize(ApplicationEngine engine, uint value)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MaxLowPriorityTransactionSize));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_FeePerByte));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            HashSet<UInt160> accounts = new HashSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!accounts.Add(account)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = accounts.ToArray().ToByteArray();
            return true;
        }

        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (!CheckValidators(engine)) return false;
            StorageKey key = CreateStorageKey(Prefix_BlockedAccounts);
            StorageItem storage = engine.Snapshot.Storages[key];
            HashSet<UInt160> accounts = new HashSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!accounts.Remove(account)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = accounts.ToArray().ToByteArray();
            return true;
        }
    }
}
