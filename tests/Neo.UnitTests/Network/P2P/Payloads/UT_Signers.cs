// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Signers.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Signers
    {
        [TestMethod]
        public void Serialize_Deserialize_Global()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.Global,
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000080";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.FromHexString().AsSerializable<Signer>();

            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CalledByEntry()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CalledByEntry,
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000001";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.FromHexString().AsSerializable<Signer>();

            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_MaxNested_And()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.WitnessRules,
                Account = UInt160.Zero,
                Rules = new WitnessRule[]{ new WitnessRule()
                {
                    Action = WitnessRuleAction.Allow,
                    Condition = new AndCondition()
                    {
                        Expressions = new WitnessCondition[]
                        {
                            new AndCondition()
                            {
                                Expressions = new WitnessCondition[]
                                {
                                    new AndCondition()
                                    {
                                        Expressions = new WitnessCondition[]
                                        {
                                            new BooleanCondition() { Expression=true }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }}
            };

            var hex = "00000000000000000000000000000000000000004001010201020102010001";
            attr.ToArray().ToHexString().Should().Be(hex);

            Assert.ThrowsException<FormatException>(() => hex.FromHexString().AsSerializable<Signer>());
        }

        [TestMethod]
        public void Serialize_Deserialize_MaxNested_Or()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.WitnessRules,
                Account = UInt160.Zero,
                Rules = new WitnessRule[]{ new WitnessRule()
                {
                    Action = WitnessRuleAction.Allow,
                    Condition = new OrCondition()
                    {
                        Expressions = new WitnessCondition[]
                        {
                            new OrCondition()
                            {
                                Expressions = new WitnessCondition[]
                                {
                                    new OrCondition()
                                    {
                                        Expressions = new WitnessCondition[]
                                        {
                                            new BooleanCondition() { Expression=true }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }}
            };

            var hex = "00000000000000000000000000000000000000004001010301030103010001";
            attr.ToArray().ToHexString().Should().Be(hex);

            Assert.ThrowsException<FormatException>(() => hex.FromHexString().AsSerializable<Signer>());
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomContracts()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = new[] { UInt160.Zero },
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000010010000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.FromHexString().AsSerializable<Signer>();

            Assert.AreEqual(attr.Scopes, copy.Scopes);
            CollectionAssert.AreEqual(attr.AllowedContracts, copy.AllowedContracts);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomGroups()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomGroups,
                AllowedGroups = new[] { ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1) },
                Account = UInt160.Zero
            };

            var hex = "0000000000000000000000000000000000000000200103b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.FromHexString().AsSerializable<Signer>();

            Assert.AreEqual(attr.Scopes, copy.Scopes);
            CollectionAssert.AreEqual(attr.AllowedGroups, copy.AllowedGroups);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Json_Global()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.Global,
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"Global\"}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CalledByEntry()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CalledByEntry,
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CalledByEntry\"}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CustomContracts()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = new[] { UInt160.Zero },
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomContracts\",\"allowedcontracts\":[\"0x0000000000000000000000000000000000000000\"]}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CustomGroups()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomGroups,
                AllowedGroups = new[] { ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1) },
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomGroups\",\"allowedgroups\":[\"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c\"]}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_From()
        {
            Signer signer = new()
            {
                Account = UInt160.Zero,
                Scopes = WitnessScope.CustomContracts | WitnessScope.CustomGroups | WitnessScope.WitnessRules,
                AllowedContracts = new[] { UInt160.Zero },
                AllowedGroups = new[] { ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1) },
                Rules = new WitnessRule[] { new() { Action = WitnessRuleAction.Allow, Condition = new BooleanCondition { Expression = true } } }
            };
            var json = signer.ToJson();
            var new_signer = Signer.FromJson(json);
            Assert.IsTrue(new_signer.Account.Equals(signer.Account));
            Assert.IsTrue(new_signer.Scopes == signer.Scopes);
            Assert.AreEqual(1, new_signer.AllowedContracts.Length);
            Assert.IsTrue(new_signer.AllowedContracts[0].Equals(signer.AllowedContracts[0]));
            Assert.AreEqual(1, new_signer.AllowedGroups.Length);
            Assert.IsTrue(new_signer.AllowedGroups[0].Equals(signer.AllowedGroups[0]));
        }
    }
}
