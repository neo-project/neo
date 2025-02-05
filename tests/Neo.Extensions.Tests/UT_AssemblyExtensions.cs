// Copyright (C) 2015-2025 The Neo Project.
//
// UT_AssemblyExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_AssemblyExtensions
    {
        [TestMethod]
        public void TestGetVersion()
        {
            // assembly without version

            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .Where(u => u.FullName == "Anonymously Hosted DynamicMethods Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                .FirstOrDefault();
            string version = asm?.GetVersion() ?? "";
            Assert.AreEqual("0.0.0", version);
        }
    }
}
