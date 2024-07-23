// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractMethodAttribute
    {
        [TestMethod]
        public void TestConstructorOneArg()
        {
            var arg = new ContractMethodAttribute();

            Assert.IsNull(arg.ActiveIn);

            arg = new ContractMethodAttribute(Hardfork.HF_Aspidochelone);

            Assert.AreEqual(Hardfork.HF_Aspidochelone, arg.ActiveIn);
        }
    }
}
