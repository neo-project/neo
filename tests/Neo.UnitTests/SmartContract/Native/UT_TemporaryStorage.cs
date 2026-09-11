// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TemporaryStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Array = System.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_TemporaryStorage
    {
        private static readonly MethodInfo PutMethod = typeof(TemporaryStorage)
            .GetMethod("Put", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GetOwnMethod = typeof(TemporaryStorage)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "Get" && m.GetParameters().Length == 2);
        private static readonly MethodInfo GetCrossMethod = typeof(TemporaryStorage)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "Get" && m.GetParameters().Length == 3);
        private static readonly MethodInfo DeleteMethod = typeof(TemporaryStorage)
            .GetMethod("Delete", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo RenewMethod = typeof(TemporaryStorage)
            .GetMethod("Renew", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo PostPersistMethod = typeof(TemporaryStorage)
            .GetMethod("PostPersistAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private DataCache _snapshot = null!;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void Activations_RequireHuyao()
        {
            Assert.Contains(Hardfork.HF_Huyao, NativeContract.TemporaryStorage.Activations);
        }

        [TestMethod]
        public void Hash_Name_And_Id()
        {
            Assert.AreEqual(nameof(TemporaryStorage), NativeContract.TemporaryStorage.Name);
            Assert.AreEqual(-12, NativeContract.TemporaryStorage.Id);
            Assert.IsTrue(NativeContract.IsNative(NativeContract.TemporaryStorage.Hash));
        }

        [TestMethod]
        public void Initialize_Defaults()
        {
            var snapshot = _snapshot.CloneCache();
            Assert.AreEqual(TemporaryStorage.DefaultMaxTtlBlocks, NativeContract.TemporaryStorage.GetMaxTtl(snapshot));
            Assert.AreEqual(TemporaryStorage.DefaultCleanupLimit, NativeContract.TemporaryStorage.GetCleanupLimit(snapshot));
        }

        [TestMethod]
        public void Put_Get_SameBlock_Visible()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var key = new byte[] { 0x01, 0x02 };
            var value = new byte[] { 0xaa, 0xbb, 0xcc };
            var block = CreateBlock(100);

            Put(snapshot, caller, key, value, ttl: 10, block);

            var got = GetOwn(snapshot, caller, key, block);
            Assert.IsNotNull(got);
            Assert.AreSequenceEqual(value, got);
        }

        [TestMethod]
        public void Get_ReturnsNull_WhenExpired_Lazy()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var key = new byte[] { 0x11 };
            var value = new byte[] { 0x22 };
            Put(snapshot, caller, key, value, ttl: 5, CreateBlock(50));

            var stillValid = GetOwn(snapshot, caller, key, CreateBlock(54));
            Assert.IsNotNull(stillValid);
            Assert.AreSequenceEqual(value, stillValid);

            Assert.IsNull(GetOwn(snapshot, caller, key, CreateBlock(55)));
        }

        [TestMethod]
        public void CrossContract_Get()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var reader = UInt160.Parse("0x0000000000000000000000000000000000000002");
            var key = new byte[] { 0xab };
            var value = new byte[] { 0xcd };
            var block = CreateBlock(10);

            Put(snapshot, owner, key, value, ttl: 20, block);

            var got = GetCross(snapshot, reader, owner, key, block);
            Assert.IsNotNull(got);
            Assert.AreSequenceEqual(value, got);
        }

        [TestMethod]
        public void Delete_RemovesEntry()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            var block = CreateBlock(1);

            Put(snapshot, caller, key, value, ttl: 100, block);
            Assert.IsTrue(Delete(snapshot, caller, key, block));
            Assert.IsNull(GetOwn(snapshot, caller, key, block));
            Assert.IsFalse(Delete(snapshot, caller, key, block));
        }

        [TestMethod]
        public void Renew_ExtendsTtl()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };

            Put(snapshot, caller, key, value, ttl: 2, CreateBlock(10));
            Assert.IsTrue(Renew(snapshot, caller, key, ttl: 10, CreateBlock(11)));

            var got = GetOwn(snapshot, caller, key, CreateBlock(20));
            Assert.IsNotNull(got);
            Assert.AreSequenceEqual(value, got);
            Assert.IsNull(GetOwn(snapshot, caller, key, CreateBlock(21)));
        }

        [TestMethod]
        public void PostPersist_CleansExpired_Bounded()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");

            for (byte i = 0; i < 3; i++)
                Put(snapshot, caller, [i], [i], ttl: 1, CreateBlock(4));

            SetCleanupLimit(snapshot, 2);

            RunPostPersist(snapshot, CreateBlock(5));
            Assert.AreEqual(1, CountDataKeys(snapshot));

            RunPostPersist(snapshot, CreateBlock(5));
            Assert.AreEqual(0, CountDataKeys(snapshot));
        }

        [TestMethod]
        public void Put_RejectsInvalidTtlAndSizes()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var block = CreateBlock(1);

            Assert.ThrowsExactly<TargetInvocationException>(() =>
                Put(snapshot, caller, [1], [1], ttl: 0, block));

            Assert.ThrowsExactly<TargetInvocationException>(() =>
                Put(snapshot, caller, new byte[TemporaryStorage.MaxKeyLength + 1], [1], ttl: 1, block));

            Assert.ThrowsExactly<TargetInvocationException>(() =>
                Put(snapshot, caller, [1], new byte[TemporaryStorage.MaxValueLength + 1], ttl: 1, block));
        }

        #region Isolation — other contracts cannot overwrite owner data

        [TestMethod]
        public void Isolation_PutSameKey_DoesNotOverwriteOtherContract()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000a1");
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000a2");
            var key = new byte[] { 0x01, 0x02, 0x03 };
            var ownerValue = new byte[] { 0xaa, 0xaa };
            var attackerValue = new byte[] { 0xbb, 0xbb };
            var block = CreateBlock(10);

            Put(snapshot, owner, key, ownerValue, ttl: 50, block);
            Put(snapshot, attacker, key, attackerValue, ttl: 50, block);

            // Same user key, separate per-caller namespaces.
            Assert.AreSequenceEqual(ownerValue, GetOwn(snapshot, owner, key, block)!);
            Assert.AreSequenceEqual(attackerValue, GetOwn(snapshot, attacker, key, block)!);
        }

        [TestMethod]
        public void Isolation_Delete_CannotRemoveOtherContractEntry()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000b1");
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000b2");
            var key = new byte[] { 0x10 };
            var value = new byte[] { 0x20 };
            var block = CreateBlock(20);

            Put(snapshot, owner, key, value, ttl: 100, block);

            Assert.IsFalse(Delete(snapshot, attacker, key, block));
            Assert.AreSequenceEqual(value, GetOwn(snapshot, owner, key, block)!);
        }

        [TestMethod]
        public void Isolation_Renew_CannotExtendOtherContractEntry()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000c1");
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000c2");
            var key = new byte[] { 0x30 };
            var value = new byte[] { 0x40 };

            // Owner: put at 10, ttl 2 => expire 12
            Put(snapshot, owner, key, value, ttl: 2, CreateBlock(10));

            // Attacker renews same key at height 11 — must not touch owner's entry.
            Assert.IsFalse(Renew(snapshot, attacker, key, ttl: 100, CreateBlock(11)));

            // Owner still expires at 12 (not extended to 111).
            Assert.AreSequenceEqual(value, GetOwn(snapshot, owner, key, CreateBlock(11))!);
            Assert.IsNull(GetOwn(snapshot, owner, key, CreateBlock(12)));
        }

        [TestMethod]
        public void Isolation_CrossGet_IsReadOnly_DoesNotGrantWrite()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000d1");
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000d2");
            var key = new byte[] { 0x50 };
            var original = new byte[] { 0x01 };
            var block = CreateBlock(5);

            Put(snapshot, owner, key, original, ttl: 40, block);

            // Cross-contract get is allowed (read).
            Assert.AreSequenceEqual(original, GetCross(snapshot, attacker, owner, key, block)!);

            // Attacker put only writes attacker's namespace; owner's value unchanged.
            Put(snapshot, attacker, key, [0xff], ttl: 40, block);
            Assert.AreSequenceEqual(original, GetOwn(snapshot, owner, key, block)!);
            Assert.AreSequenceEqual(original, GetCross(snapshot, attacker, owner, key, block)!);
        }

        [TestMethod]
        public void Isolation_SystemStoragePut_CannotWriteTemporaryStorageId()
        {
            // System.Storage.GetContext always binds Id to the calling contract.
            // A foreign contract's StorageContext cannot target TemporaryStorage.Id (-12).
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000e1");
            var key = new byte[] { 0x60 };
            var value = new byte[] { 0x70 };
            var block = CreateBlock(8);

            Put(snapshot, owner, key, value, ttl: 30, block);

            // Simulate another contract (positive id) writing with the same raw key bytes.
            // Storage is partitioned by contract Id, so TemporaryStorage (id -12) is untouched.
            using (var engine = CreateEngine(snapshot, owner, block))
            {
                var foreignCtx = new StorageContext { Id = 1, IsReadOnly = false };
                // Build a key that looks like TemporaryStorage's on-disk layout (prefix + hash + user key).
                // Even with identical key bytes under id=1, TemporaryStorage.Id data must remain.
                Span<byte> crafted = stackalloc byte[1 + UInt160.Length + key.Length];
                crafted[0] = 1; // Prefix_Data
                owner.GetSpan().CopyTo(crafted[1..]);
                key.CopyTo(crafted[(1 + UInt160.Length)..]);
                engine.Put(foreignCtx, crafted.ToArray(), [0xde, 0xad]);
            }

            Assert.AreSequenceEqual(value, GetOwn(snapshot, owner, key, block)!);
            Assert.AreEqual(1, CountDataKeys(snapshot)); // still only owner's TemporaryStorage entry
        }

        [TestMethod]
        public void Isolation_PolicyKeys_NotOverwriteableViaPut()
        {
            // put() always uses Prefix_Data + caller hash; it cannot address Prefix_MaxTtl / Prefix_CleanupLimit.
            var snapshot = _snapshot.CloneCache();
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000f1");
            var block = CreateBlock(3);

            var maxBefore = NativeContract.TemporaryStorage.GetMaxTtl(snapshot);
            var limitBefore = NativeContract.TemporaryStorage.GetCleanupLimit(snapshot);

            // Try keys that match policy prefix bytes if used as bare user keys.
            Put(snapshot, attacker, [10], [0xff, 0xff], ttl: 5, block);
            Put(snapshot, attacker, [11], [0xff, 0xff], ttl: 5, block);

            Assert.AreEqual(maxBefore, NativeContract.TemporaryStorage.GetMaxTtl(snapshot));
            Assert.AreEqual(limitBefore, NativeContract.TemporaryStorage.GetCleanupLimit(snapshot));
        }

        [TestMethod]
        public void Vm_Isolation_PutSameKey_SeparateNamespaces()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x00000000000000000000000000000000000000a3");
            var attacker = UInt160.Parse("0x00000000000000000000000000000000000000a4");
            var key = new byte[] { 0x99 };
            var ownerValue = new byte[] { 0x11 };
            var attackerValue = new byte[] { 0x22 };
            var block = CreateBlock(15);

            ExecuteVmHalt(snapshot, owner, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, ownerValue, 20u);
            });
            ExecuteVmHalt(snapshot, attacker, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, attackerValue, 20u);
            });

            // Attacker delete must not remove owner's entry.
            var deleted = ExecuteVmHalt(snapshot, attacker, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "delete", key);
            });
            Assert.IsTrue(deleted.GetBoolean()); // deletes attacker's own entry

            var ownerStill = ExecuteVmHalt(snapshot, owner, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsInstanceOfType<ByteString>(ownerStill);
            Assert.AreSequenceEqual(ownerValue, ownerStill.GetSpan().ToArray());

            var attackerGone = ExecuteVmHalt(snapshot, attacker, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsTrue(attackerGone.IsNull);
        }

        #endregion

        #region ApplicationEngine / VM path

        [TestMethod]
        public void Vm_Put_Get_SameBlock_ViaDynamicCall()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000010");
            var key = new byte[] { 0x01, 0x02 };
            var value = new byte[] { 0xde, 0xad, 0xbe, 0xef };
            var block = CreateBlock(200);

            ExecuteVmHalt(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 15u);
            });

            var result = ExecuteVmHalt(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });

            Assert.IsInstanceOfType<ByteString>(result);
            Assert.AreSequenceEqual(value, result.GetSpan().ToArray());
        }

        [TestMethod]
        public void Vm_Put_Get_InSingleScript()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000011");
            var key = new byte[] { 0x42 };
            var value = new byte[] { 0x11, 0x22, 0x33 };
            var block = CreateBlock(50);

            // Same-block put then get in one ApplicationEngine execution.
            var result = ExecuteVmHalt(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 8u);
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });

            Assert.IsInstanceOfType<ByteString>(result);
            Assert.AreSequenceEqual(value, result.GetSpan().ToArray());
        }

        [TestMethod]
        public void Vm_Get_CrossContract_ViaDynamicCall()
        {
            var snapshot = _snapshot.CloneCache();
            var owner = UInt160.Parse("0x0000000000000000000000000000000000000020");
            var reader = UInt160.Parse("0x0000000000000000000000000000000000000021");
            var key = new byte[] { 0xab };
            var value = new byte[] { 0xcd, 0xef };
            var block = CreateBlock(30);

            ExecuteVmHalt(snapshot, owner, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 25u);
            });

            var result = ExecuteVmHalt(snapshot, reader, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", owner, key);
            });

            Assert.IsInstanceOfType<ByteString>(result);
            Assert.AreSequenceEqual(value, result.GetSpan().ToArray());
        }

        [TestMethod]
        public void Vm_Get_Missing_ReturnsNull()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000030");
            var block = CreateBlock(1);

            var result = ExecuteVmHalt(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", new byte[] { 0xff });
            });

            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void Vm_Delete_And_Renew_ViaDynamicCall()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000040");
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };

            ExecuteVmHalt(snapshot, caller, CreateBlock(10), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 2u);
            });

            // renew at height 11 with ttl 10 => expire 21
            var renewed = ExecuteVmHalt(snapshot, caller, CreateBlock(11), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "renew", key, 10u);
            });
            Assert.IsTrue(renewed.GetBoolean());

            var stillThere = ExecuteVmHalt(snapshot, caller, CreateBlock(20), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsInstanceOfType<ByteString>(stillThere);
            Assert.AreSequenceEqual(value, stillThere.GetSpan().ToArray());

            var deleted = ExecuteVmHalt(snapshot, caller, CreateBlock(20), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "delete", key);
            });
            Assert.IsTrue(deleted.GetBoolean());

            var afterDelete = ExecuteVmHalt(snapshot, caller, CreateBlock(20), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsTrue(afterDelete.IsNull);
        }

        [TestMethod]
        public void Vm_GetMaxTtl_And_GetCleanupLimit()
        {
            var snapshot = _snapshot.CloneCache();
            var block = CreateBlock(1);

            // Read-only methods do not require a spoofed caller.
            var maxTtl = ExecuteVmHalt(snapshot, caller: null, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "getMaxTtl");
            });
            Assert.AreEqual(TemporaryStorage.DefaultMaxTtlBlocks, (uint)(int)maxTtl.GetInteger());

            var limit = ExecuteVmHalt(snapshot, caller: null, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "getCleanupLimit");
            });
            Assert.AreEqual(TemporaryStorage.DefaultCleanupLimit, (uint)(int)limit.GetInteger());
        }

        [TestMethod]
        public void Vm_Put_InvalidTtl_Faults()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000050");
            var block = CreateBlock(1);

            using var engine = ExecuteVm(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", new byte[] { 1 }, new byte[] { 1 }, 0u);
            }, out var state);

            Assert.AreEqual(VMState.FAULT, state);
            Assert.IsNotNull(engine.FaultException);
        }

        [TestMethod]
        public void Vm_Put_OversizedKey_Faults()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000051");
            var block = CreateBlock(1);
            var bigKey = new byte[TemporaryStorage.MaxKeyLength + 1];

            using var engine = ExecuteVm(snapshot, caller, block, sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", bigKey, new byte[] { 1 }, 1u);
            }, out var state);

            Assert.AreEqual(VMState.FAULT, state);
            Assert.IsNotNull(engine.FaultException);
        }

        [TestMethod]
        public void Vm_Put_EmitsNotification()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000060");
            var key = new byte[] { 0x99 };
            var value = new byte[] { 0x01 };
            var block = CreateBlock(100);
            var notifications = new List<NotifyEventArgs>();

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block,
                TestProtocolSettings.Default, gas: 100_0000_0000);
            engine.Notify += (_, e) => notifications.Add(e);

            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 7u);
            engine.LoadScript(sb.ToArray());
            engine.CurrentContext!.GetState<ExecutionContextState>().ScriptHash = caller;

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var putEvents = notifications.Where(n =>
                n.ScriptHash.Equals(NativeContract.TemporaryStorage.Hash) && n.EventName == "Put").ToList();
            Assert.HasCount(1, putEvents);
            Assert.HasCount(3, putEvents[0].State); // contract, key, expireHeight
            // expireTime = block.Timestamp + ttlBlocks * Policy.GetMillisecondsPerBlock (default 15s)
            // 100 * 15_000 + 7 * 15_000 = 1_605_000
            Assert.AreEqual(1_605_000, (int)putEvents[0].State[2].GetInteger());
        }

        [TestMethod]
        public void Ttl_UsesPolicyMillisecondsPerBlock()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x00000000000000000000000000000000000000aa");
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };

            // Default ms/block is 15_000. Put at ts=1_000_000 with ttl=2 => expire at 1_030_000.
            var putBlock = CreateBlock(0);
            putBlock.Header.Timestamp = 1_000_000;
            Put(snapshot, caller, key, value, ttl: 2, putBlock);

            var beforeExpire = CreateBlock(0);
            beforeExpire.Header.Timestamp = 1_029_999;
            Assert.AreSequenceEqual(value, GetOwn(snapshot, caller, key, beforeExpire)!);

            var atExpire = CreateBlock(0);
            atExpire.Header.Timestamp = 1_030_000;
            Assert.IsNull(GetOwn(snapshot, caller, key, atExpire));
        }

        [TestMethod]
        public void Vm_PostPersist_Gc_ViaNativePostPersistSyscall()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000070");

            for (byte i = 0; i < 3; i++)
            {
                ExecuteVmHalt(snapshot, caller, CreateBlock(4), sb =>
                {
                    sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", new byte[] { i }, new byte[] { i }, 1u);
                });
            }

            SetCleanupLimit(snapshot, 2);
            Assert.AreEqual(3, CountDataKeys(snapshot));

            // System.Contract.NativePostPersist runs TemporaryStorage.PostPersistAsync through the VM.
            using (var sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                using var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, CreateBlock(5),
                    TestProtocolSettings.Default, gas: 100_0000_0000);
                engine.LoadScript(sb.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            }

            Assert.AreEqual(1, CountDataKeys(snapshot));

            using (var sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                using var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, CreateBlock(5),
                    TestProtocolSettings.Default, gas: 100_0000_0000);
                engine.LoadScript(sb.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            }

            Assert.AreEqual(0, CountDataKeys(snapshot));
        }

        [TestMethod]
        public void Vm_Put_WithoutStatesFlag_Faults()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000080");
            var block = CreateBlock(1);

            // Entry CallFlags.None => DynamicCall cannot grant States to TemporaryStorage.put.
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block,
                TestProtocolSettings.Default, gas: 100_0000_0000);
            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put",
                CallFlags.None, new byte[] { 1 }, new byte[] { 1 }, 1u);
            engine.LoadScript(sb.ToArray());
            engine.CurrentContext!.GetState<ExecutionContextState>().ScriptHash = caller;
            engine.CurrentContext.GetState<ExecutionContextState>().CallFlags = CallFlags.None;

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsNotNull(engine.FaultException);
        }

        [TestMethod]
        public void Vm_Get_Expired_ReturnsNull()
        {
            var snapshot = _snapshot.CloneCache();
            var caller = UInt160.Parse("0x0000000000000000000000000000000000000090");
            var key = new byte[] { 0x11 };
            var value = new byte[] { 0x22 };

            ExecuteVmHalt(snapshot, caller, CreateBlock(50), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "put", key, value, 5u);
            });

            var valid = ExecuteVmHalt(snapshot, caller, CreateBlock(54), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsInstanceOfType<ByteString>(valid);
            Assert.AreSequenceEqual(value, valid.GetSpan().ToArray());

            var expired = ExecuteVmHalt(snapshot, caller, CreateBlock(55), sb =>
            {
                sb.EmitDynamicCall(NativeContract.TemporaryStorage.Hash, "get", key);
            });
            Assert.IsTrue(expired.IsNull);
        }

        #endregion

        private static Block CreateBlock(uint index) => new()
        {
            Header = new Header
            {
                Index = index,
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                Witness = Witness.Empty,
                Timestamp = index * 15_000UL,
            },
            Transactions = []
        };

        private static ApplicationEngine CreateEngine(DataCache snapshot, UInt160 caller, Block block)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block, TestProtocolSettings.Default, gas: 100_0000_0000);
            // Share the caller's snapshot (do not keep the default CloneCache) so writes persist after dispose.
            engine.LoadScript(new Script(new byte[] { (byte)OpCode.RET }), configureState: state =>
            {
                state.SnapshotCache = snapshot;
                state.ScriptHash = caller;
                state.NativeCallingScriptHash = caller;
                state.CallFlags = CallFlags.All;
            });
            return engine;
        }

        private static void Put(DataCache snapshot, UInt160 caller, byte[] key, byte[] value, uint ttl, Block block)
        {
            using var engine = CreateEngine(snapshot, caller, block);
            try
            {
                PutMethod.Invoke(NativeContract.TemporaryStorage, [engine, key, value, ttl]);
            }
            catch (TargetInvocationException)
            {
                throw;
            }
        }

        private static byte[]? GetOwn(DataCache snapshot, UInt160 caller, byte[] key, Block block)
        {
            using var engine = CreateEngine(snapshot, caller, block);
            return (byte[]?)GetOwnMethod.Invoke(NativeContract.TemporaryStorage, [engine, key]);
        }

        private static byte[]? GetCross(DataCache snapshot, UInt160 caller, UInt160 owner, byte[] key, Block block)
        {
            using var engine = CreateEngine(snapshot, caller, block);
            return (byte[]?)GetCrossMethod.Invoke(NativeContract.TemporaryStorage, [engine, owner, key]);
        }

        private static bool Delete(DataCache snapshot, UInt160 caller, byte[] key, Block block)
        {
            using var engine = CreateEngine(snapshot, caller, block);
            return (bool)DeleteMethod.Invoke(NativeContract.TemporaryStorage, [engine, key])!;
        }

        private static bool Renew(DataCache snapshot, UInt160 caller, byte[] key, uint ttl, Block block)
        {
            using var engine = CreateEngine(snapshot, caller, block);
            return (bool)RenewMethod.Invoke(NativeContract.TemporaryStorage, [engine, key, ttl])!;
        }

        private static void SetCleanupLimit(DataCache snapshot, uint value)
        {
            var key = new KeyBuilder(NativeContract.TemporaryStorage.Id, 11);
            snapshot.GetAndChange(key, () => new StorageItem(value))!.Set(value);
        }

        private static void RunPostPersist(DataCache snapshot, Block block)
        {
            using var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block, TestProtocolSettings.Default);
            var result = PostPersistMethod.Invoke(NativeContract.TemporaryStorage, [engine]);
            if (result is ContractTask task)
                task.GetAwaiter().GetResult();
        }

        private static int CountDataKeys(DataCache snapshot)
        {
            var prefix = StorageKey.Create(NativeContract.TemporaryStorage.Id, 1);
            return snapshot.Find(prefix).Count();
        }

        /// <summary>
        /// Runs a script through <see cref="ApplicationEngine"/> and asserts HALT.
        /// Spoofs entry <see cref="ExecutionContextState.ScriptHash"/> so TemporaryStorage
        /// scopes puts/gets to a stable caller identity across DynamicCalls.
        /// </summary>
        private static StackItem ExecuteVmHalt(DataCache snapshot, UInt160? caller, Block block, Action<ScriptBuilder> build)
        {
            using var engine = ExecuteVm(snapshot, caller, block, build, out var state);
            Assert.AreEqual(VMState.HALT, state, engine.FaultException?.ToString());
            return engine.ResultStack.Count > 0 ? engine.ResultStack.Pop() : StackItem.Null;
        }

        private static ApplicationEngine ExecuteVm(DataCache snapshot, UInt160? caller, Block block, Action<ScriptBuilder> build, out VMState state)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block,
                TestProtocolSettings.Default, gas: 100_0000_0000);
            using var sb = new ScriptBuilder();
            build(sb);
            engine.LoadScript(sb.ToArray());
            if (caller is not null)
            {
                var ctx = engine.CurrentContext!.GetState<ExecutionContextState>();
                ctx.ScriptHash = caller;
                ctx.CallFlags = CallFlags.All;
            }
            state = engine.Execute();
            return engine;
        }
    }
}
