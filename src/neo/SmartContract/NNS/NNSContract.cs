using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Array = Neo.VM.Types.Array;
using Neo.Cryptography;
using System.Text.RegularExpressions;

namespace Neo.SmartContract.NNS
{
    public partial class NNSContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.NNS";
        public override int Id => -5;
        public string Name => "NNS";
        public string Symbol => "nns";
        public byte Decimals => 0;
        public const string DomainRegex = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62}){1,3}$";
        public const string RootRegex = @"^[a-zA-Z]{0,62}$";

        protected const byte Prefix_Domain = 23;
        protected const byte Prefix_Record = 24;
        protected const byte Prefix_OwnershipMapping = 25;
        protected const byte Prefix_Admin = 26;
        protected const byte Prefix_RentalPrice = 27;

        private bool CheckValidators(ApplicationEngine engine)
        {
            UInt256 prev_hash = engine.Snapshot.PersistingBlock.PrevHash;
            TrimmedBlock prev_block = engine.Snapshot.Blocks[prev_hash];
            return InteropService.Runtime.CheckWitnessInternal(engine, prev_block.NextConsensus);
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Admin), new StorageItem
            {
                Value = new UInt160[0].ToByteArray()
            });
            return true;
        }

        public static bool IsDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            Regex regex = new Regex(DomainRegex);
            return regex.Match(name).Success;
        }

        public static bool IsRootDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            Regex regex = new Regex(RootRegex);
            return regex.Match(name).Success;
        }

        //注册域名
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "name", "owner", "admin", "ttl" })]
        public StackItem RegisterNewName(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name)));
            UInt160 owner = new UInt160(args[1].GetSpan());
            UInt160 admin = new UInt160(args[2].GetSpan());
            uint ttl = (uint)args[3].GetBigInteger();
            // TODO: verify format, get root or first level domain check witness
            if (IsRootDomain(name) && !CheckValidators(engine))
            {
                return false;
            }

            if (IsDomain(name))
            {
                var levels = name.Split(".");

                // Register first level
                if (levels.Length == 2)
                {
                    // check owner of root
                    string root = levels[^1];

                }
                else
                {
                    // check owner of first level
                    UInt256 firstLevel = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(levels[^2])));
                    var domainInfo = GetDomainInfo(engine, firstLevel);
                    if (domainInfo is null) return false;
                    if (!InteropService.Runtime.CheckWitnessInternal(engine, domainInfo.Owner) &&
                        !InteropService.Runtime.CheckWitnessInternal(engine, domainInfo.Admin))
                        return false;
                }

                // TOTD: 

                return false;
            }

            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];

            // TODO: what if ttl is expired

            if (storage is null)
            {
                DomainInfo domainInfo = new DomainInfo { Admin = admin, Owner = owner, TimeToLive = ttl, Name = name };
                engine.Snapshot.Storages.Add(key, new StorageItem
                {
                    Value = domainInfo.ToArray()
                });

                UpdateOwnerShip(engine, name, owner);
            }
            return true;
        }

        private DomainInfo GetDomainInfo(ApplicationEngine engine, UInt256 nameHash)
        {
            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage is null) return null;
            return storage.Value.AsSerializable<DomainInfo>();
        }

        //更新一级域名有效期，任何人都可以调用该接口 
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "ttl" })]
        private StackItem RenewName(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name)));
            uint ttl = (uint)args[1].GetBigInteger();
            // TODO: verify format

            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            DomainInfo domainInfo = storage.Value.AsSerializable<DomainInfo>();
            if (domainInfo is null) return false;
            domainInfo.TimeToLive = ttl;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }

        // 设置管理员，委员会有权限调用
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "address" })]
        private StackItem SetAdmin(ApplicationEngine engine, Array args)
        {
            //TODO: 验证委员会多签
            UInt160 address = new UInt160(args[0].GetSpan());
            StorageKey key = CreateStorageKey(Prefix_Admin);
            StorageItem storage = engine.Snapshot.Storages[key];
            SortedSet<UInt160> admins = new SortedSet<UInt160>(storage.Value.AsSerializableArray<UInt160>());
            if (!admins.Add(address)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = admins.ToByteArray();
            return true;
        }

        // 设置租赁价格
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRentalPrice(ApplicationEngine engine, Array args)
        {
            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RentalPrice));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        // 设置域名所有者owner，只有当前owner才可以调用
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.String }, ParameterNames = new[] { "from", "to", "name" })]
        private StackItem Transfer(ApplicationEngine engine, Array args)
        {
            UInt160 from = new UInt160(args[0].GetSpan());
            UInt160 to = new UInt160(args[1].GetSpan());
            string name = args[2].GetString().ToLower();

            if (!from.Equals(engine.CallingScriptHash) && !InteropService.Runtime.CheckWitnessInternal(engine, from))
                return false;
            SetOwner(engine, name, to);
            UpdateOwnerShip(engine, name, to);
            UpdateOwnerShip(engine, name, from, false);
            return true;
        }
    }
}
