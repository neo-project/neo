// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ScryptParameters.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Wallets.NEP6;

namespace Neo.UnitTests.Wallets.NEP6
{
    [TestClass]
    public class UT_ScryptParameters
    {
        ScryptParameters uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = ScryptParameters.Default;
        }

        [TestMethod]
        public void Test_Default_ScryptParameters()
        {
            Assert.AreEqual(16384, uut.N);
            Assert.AreEqual(8, uut.R);
            Assert.AreEqual(8, uut.P);
        }

        [TestMethod]
        public void Test_ScryptParameters_Default_ToJson()
        {
            JObject json = ScryptParameters.Default.ToJson();
            Assert.AreEqual(ScryptParameters.Default.N, json["n"].AsNumber());
            Assert.AreEqual(ScryptParameters.Default.R, json["r"].AsNumber());
            Assert.AreEqual(ScryptParameters.Default.P, json["p"].AsNumber());
        }

        [TestMethod]
        public void Test_Default_ScryptParameters_FromJson()
        {
            JObject json = new JObject();
            json["n"] = 16384;
            json["r"] = 8;
            json["p"] = 8;

            ScryptParameters uut2 = ScryptParameters.FromJson(json);
            Assert.AreEqual(ScryptParameters.Default.N, uut2.N);
            Assert.AreEqual(ScryptParameters.Default.R, uut2.R);
            Assert.AreEqual(ScryptParameters.Default.P, uut2.P);
        }

        [TestMethod]
        public void TestScryptParametersConstructor()
        {
            int n = 1, r = 2, p = 3;
            ScryptParameters parameter = new ScryptParameters(n, r, p);
            Assert.AreEqual(n, parameter.N);
            Assert.AreEqual(r, parameter.R);
            Assert.AreEqual(p, parameter.P);
        }
    }
}
