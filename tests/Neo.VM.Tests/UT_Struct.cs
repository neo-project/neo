// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Struct.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.Test
{
    [TestClass]
    public class UT_Struct
    {
        private readonly Struct @struct;

        public UT_Struct()
        {
            @struct = new Struct { 1 };
            for (int i = 0; i < 20000; i++)
                @struct = new Struct { @struct };
        }

        [TestMethod]
        public void TestClone()
        {
            Struct s1 = new() { 1, new Struct { 2 } };
            Struct s2 = s1.Clone(ExecutionEngineLimits.Default);
            s1[0] = 3;
            Assert.AreEqual(1, s2[0]);
            ((Struct)s1[1])[0] = 3;
            Assert.AreEqual(2, ((Struct)s2[1])[0]);
            Assert.ThrowsException<InvalidOperationException>(() => @struct.Clone(ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void TestEquals()
        {
            Struct s1 = new() { 1, new Struct { 2 } };
            Struct s2 = new() { 1, new Struct { 2 } };
            Assert.IsTrue(s1.Equals(s2, ExecutionEngineLimits.Default));
            Struct s3 = new() { 1, new Struct { 3 } };
            Assert.IsFalse(s1.Equals(s3, ExecutionEngineLimits.Default));
            Assert.ThrowsException<InvalidOperationException>(() => @struct.Equals(@struct.Clone(ExecutionEngineLimits.Default), ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void TestEqualsDos()
        {
            string payloadStr = new string('h', 65535);
            Struct s1 = new();
            Struct s2 = new();
            for (int i = 0; i < 2; i++)
            {
                s1.Add(payloadStr);
                s2.Add(payloadStr);
            }
            Assert.ThrowsException<InvalidOperationException>(() => s1.Equals(s2, ExecutionEngineLimits.Default));

            for (int i = 0; i < 1000; i++)
            {
                s1.Add(payloadStr);
                s2.Add(payloadStr);
            }
            Assert.ThrowsException<InvalidOperationException>(() => s1.Equals(s2, ExecutionEngineLimits.Default));
        }
    }
}
