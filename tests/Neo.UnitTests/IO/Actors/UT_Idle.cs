// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Idle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Actors;

namespace Neo.UnitTests.IO.Actors
{
    [TestClass]
    public class UT_Idle
    {
        [TestMethod]
        public void Instance_IsSingleton()
        {
            Assert.IsNotNull(Idle.Instance);
            Assert.AreSame(Idle.Instance, Idle.Instance);
        }
    }
}
