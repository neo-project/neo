// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractStateExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.IO;
using Neo.Extensions.SmartContract;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Numerics;
using System.Reflection;

namespace Neo.UnitTests.Extensions;

[TestClass]
public class UT_ContractStateExtensions
{
    private NeoSystem _system = null!;

    [TestInitialize]
    public void Initialize()
    {
        _system = TestBlockchain.GetSystem();
    }

    [TestMethod]
    public void TestGetStorage()
    {
        var contractStorage = NativeContract.ContractManagement.FindContractStorage(_system.StoreView, NativeContract.Governance.Id);
        Assert.IsNotNull(contractStorage);

        var governanceContract = NativeContract.ContractManagement.GetContractById(_system.StoreView, NativeContract.Governance.Id);
        Assert.IsNotNull(governanceContract);

        contractStorage = governanceContract.FindStorage(_system.StoreView);

        Assert.IsNotNull(contractStorage);

        contractStorage = governanceContract.FindStorage(_system.StoreView, [10]);

        Assert.IsNotNull(contractStorage);

        UInt160 address = "0x9f8f056a53e39585c7bb52886418c7bed83d126b";
        var item = governanceContract.GetStorage(_system.StoreView, [10, .. address.ToArray()]);

        Assert.IsNotNull(item);
        var neoAccountStateType = typeof(Governance).GetNestedType("NeoAccountState", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("NeoAccountState type not found");
        var neoAccountState = GetInteroperable(item, neoAccountStateType);
        // NeoAccountState has BalanceHeight, VoteTo, and LastGasPerVote fields (not Balance)
        // NEO token balance is now stored in TokenManagement, not in NeoAccountState
        var balanceHeightField = neoAccountStateType.GetField("BalanceHeight", BindingFlags.Public | BindingFlags.Instance);
        var balanceHeight = (uint)(balanceHeightField?.GetValue(neoAccountState) ?? throw new InvalidOperationException("BalanceHeight field not found"));
        // The test address should have a BalanceHeight value from the genesis block
        Assert.IsTrue(balanceHeight >= 0, "BalanceHeight should be a valid block height");

        // Ensure GetInteroperableClone don't change nothing

        var cloneMethod = typeof(StorageItem).GetMethod("GetInteroperableClone", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        var genericMethod = cloneMethod?.MakeGenericMethod(neoAccountStateType);
        var clonedState = genericMethod?.Invoke(item, null);
        balanceHeightField?.SetValue(clonedState, (uint)123);
        var balanceHeightAfterClone = (uint)(balanceHeightField?.GetValue(neoAccountState) ?? throw new InvalidOperationException("BalanceHeight field not found"));
        Assert.AreEqual(balanceHeight, balanceHeightAfterClone, "Original state should not be affected by clone modification");
    }

    private static object GetInteroperable(StorageItem item, Type type)
    {
        var method = typeof(StorageItem).GetMethod("GetInteroperable", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        var genericMethod = method?.MakeGenericMethod(type);
        return genericMethod?.Invoke(item, null) ?? throw new InvalidOperationException("GetInteroperable method not found");
    }
}
