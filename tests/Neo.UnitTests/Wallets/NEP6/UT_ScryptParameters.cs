// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ScryptParameters.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
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
            uut.N.Should().Be(16384);
            uut.R.Should().Be(8);
            uut.P.Should().Be(8);
        }

        [TestMethod]
        public void Test_ScryptParameters_Default_ToJson()
        {
            JObject json = ScryptParameters.Default.ToJson();
            json["n"].AsNumber().Should().Be(ScryptParameters.Default.N);
            json["r"].AsNumber().Should().Be(ScryptParameters.Default.R);
            json["p"].AsNumber().Should().Be(ScryptParameters.Default.P);
        }

        [TestMethod]
        public void Test_Default_ScryptParameters_FromJson()
        {
            JObject json = new JObject();
            json["n"] = 16384;
            json["r"] = 8;
            json["p"] = 8;

            ScryptParameters uut2 = ScryptParameters.FromJson(json);
            uut2.N.Should().Be(ScryptParameters.Default.N);
            uut2.R.Should().Be(ScryptParameters.Default.R);
            uut2.P.Should().Be(ScryptParameters.Default.P);
        }

        [TestMethod]
        public void TestScryptParametersConstructor()
        {
            int n = 1, r = 2, p = 3;
            ScryptParameters parameter = new ScryptParameters(n, r, p);
            parameter.N.Should().Be(n);
            parameter.R.Should().Be(r);
            parameter.P.Should().Be(p);
        }
    }
}
