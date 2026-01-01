// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WitnessConditionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Builders;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.UnitTests.Builders;

[TestClass]
public class UT_WitnessConditionBuilder
{
    [TestMethod]
    public void TestAndCondition()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .And(and =>
            {
                and.CalledByContract(expectedContractHash);
                and.CalledByGroup(expectedPublicKey);
            })
            .Build();

        Assert.IsInstanceOfType<AndCondition>(condition, out var actual);
        Assert.HasCount(2, actual.Expressions);
        Assert.IsInstanceOfType<CalledByContractCondition>(actual.Expressions[0], out var exp0);
        Assert.IsInstanceOfType<CalledByGroupCondition>(actual.Expressions[1], out var exp1);
        Assert.AreEqual(expectedContractHash, exp0.Hash);
        Assert.AreEqual(expectedPublicKey, exp1.Group);
    }

    [TestMethod]
    public void TestOrCondition()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .Or(or =>
            {
                or.CalledByContract(expectedContractHash);
                or.CalledByGroup(expectedPublicKey);
            })
            .Build();

        Assert.IsInstanceOfType<OrCondition>(condition, out var actual);
        Assert.HasCount(2, actual.Expressions);
        Assert.IsInstanceOfType<CalledByContractCondition>(actual.Expressions[0], out var exp0);
        Assert.IsInstanceOfType<CalledByGroupCondition>(actual.Expressions[1], out var exp1);
        Assert.AreEqual(expectedContractHash, exp0.Hash);
        Assert.AreEqual(expectedPublicKey, exp1.Group);
    }

    [TestMethod]
    public void TestBoolean()
    {
        var condition = WitnessConditionBuilder.Create()
            .Boolean(true)
            .Build();

        var actual = condition as BooleanCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<BooleanCondition>(condition);
        Assert.IsTrue(actual.Expression);
    }

    [TestMethod]
    public void TestCalledByContract()
    {
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .CalledByContract(expectedContractHash)
            .Build();

        var actual = condition as CalledByContractCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<CalledByContractCondition>(condition);
        Assert.AreEqual(expectedContractHash, actual.Hash);
    }

    [TestMethod]
    public void TestCalledByEntry()
    {
        var condition = WitnessConditionBuilder.Create()
            .CalledByEntry()
            .Build();

        var actual = condition as CalledByEntryCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<CalledByEntryCondition>(condition);
    }

    [TestMethod]
    public void TestCalledByGroup()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var condition = WitnessConditionBuilder.Create()
            .CalledByGroup(expectedPublicKey)
            .Build();

        var actual = condition as CalledByGroupCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<CalledByGroupCondition>(condition);
        Assert.AreEqual(expectedPublicKey, actual.Group);
    }

    [TestMethod]
    public void TestGroup()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var condition = WitnessConditionBuilder.Create()
            .Group(expectedPublicKey)
            .Build();

        var actual = condition as GroupCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<GroupCondition>(condition);
        Assert.AreEqual(expectedPublicKey, actual.Group);
    }

    [TestMethod]
    public void TestScriptHash()
    {
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .ScriptHash(expectedContractHash)
            .Build();

        var actual = condition as ScriptHashCondition;

        Assert.IsNotNull(actual);
        Assert.IsInstanceOfType<ScriptHashCondition>(condition);
        Assert.AreEqual(expectedContractHash, actual.Hash);
    }

    [TestMethod]
    public void TestNotConditionWithAndCondition()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .Not(not =>
            {
                not.And(and =>
                {
                    and.CalledByContract(expectedContractHash);
                    and.CalledByGroup(expectedPublicKey);
                });
            })
            .Build();

        Assert.IsInstanceOfType<NotCondition>(condition, out var actual);
        Assert.IsInstanceOfType<AndCondition>(actual.Expression, out var actualAndCondition);
        Assert.HasCount(2, actualAndCondition.Expressions);
        Assert.IsInstanceOfType<CalledByContractCondition>(actualAndCondition.Expressions[0], out var exp0);
        Assert.IsInstanceOfType<CalledByGroupCondition>(actualAndCondition.Expressions[1], out var exp1);
        Assert.AreEqual(expectedContractHash, exp0.Hash);
        Assert.AreEqual(expectedPublicKey, exp1.Group);
    }

    [TestMethod]
    public void TestNotConditionWithOrCondition()
    {
        var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
        var expectedContractHash = UInt160.Zero;
        var condition = WitnessConditionBuilder.Create()
            .Not(not =>
            {
                not.Or(or =>
                {
                    or.CalledByContract(expectedContractHash);
                    or.CalledByGroup(expectedPublicKey);
                });
            })
            .Build();

        Assert.IsInstanceOfType<NotCondition>(condition, out var actual);
        Assert.IsInstanceOfType<OrCondition>(actual.Expression, out var actualOrCondition);
        Assert.HasCount(2, actualOrCondition.Expressions);
        Assert.IsInstanceOfType<CalledByContractCondition>(actualOrCondition.Expressions[0], out var exp0);
        Assert.IsInstanceOfType<CalledByGroupCondition>(actualOrCondition.Expressions[1], out var exp1);
        Assert.AreEqual(expectedContractHash, exp0.Hash);
        Assert.AreEqual(expectedPublicKey, exp1.Group);
    }
}
