// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Reflection;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_ContractMethodAttribute
{
    [TestMethod]
    public void TestConstructorOneArg()
    {
        var arg = new ContractMethodAttribute();
        Assert.IsNull(arg.ActiveIn);
    }

    class NeedSnapshot
    {
        [ContractMethod]
        public static bool MethodReadOnlyStoreView(IReadOnlyStore view) => view is null;

        [ContractMethod]
        public static bool MethodDataCache(DataCache dataCache) => dataCache is null;
    }

    class NoNeedSnapshot
    {
        [ContractMethod]
        public static bool MethodTwo(ApplicationEngine engine, UInt160 account)
            => engine is null || account is null;

        [ContractMethod]
        public static bool MethodOne(ApplicationEngine engine) => engine is null;
    }

    [TestMethod]
    public void TestNeedSnapshot()
    {
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var member in typeof(NeedSnapshot).GetMembers(flags))
        {
            foreach (var attribute in member.GetCustomAttributes<ContractMethodAttribute>())
            {
                var metadata = new ContractMethodMetadata(member, attribute);
                Assert.IsTrue(metadata.NeedSnapshot);
            }
        }

        foreach (var member in typeof(NoNeedSnapshot).GetMembers(flags))
        {
            foreach (var attribute in member.GetCustomAttributes<ContractMethodAttribute>())
            {
                var metadata = new ContractMethodMetadata(member, attribute);
                Assert.IsFalse(metadata.NeedSnapshot);
                Assert.IsTrue(metadata.NeedApplicationEngine);
            }
        }
    }
}
