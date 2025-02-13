// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WitnessCondition.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads.Conditions;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_WitnessCondition
    {
        [TestMethod]
        public void Test_IEquatable_ScriptHashCondition()
        {
            var expected = new ScriptHashCondition
            {
                Hash = UInt160.Zero,
            };

            var actual = new ScriptHashCondition
            {
                Hash = UInt160.Zero,
            };

            var notEqual = new ScriptHashCondition
            {
                Hash = UInt160.Parse("0xfff4f52ca43d6bf4fec8647a60415b183303d961"),
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_GroupCondition()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var expected = new GroupCondition
            {
                Group = point,
            };

            var actual = new GroupCondition
            {
                Group = point,
            };

            var notEqual = new GroupCondition
            {
                Group = ECPoint.Parse("03b209fd4f53a7170ea4444e0ca0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1),
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_CalledByGroupCondition()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var expected = new CalledByGroupCondition
            {
                Group = point,
            };

            var actual = new CalledByGroupCondition
            {
                Group = point,
            };

            var notEqual = new CalledByGroupCondition
            {
                Group = ECPoint.Parse("03b209fd4f53a7170ea4444e0ca0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1),
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_CalledByEntryCondition()
        {
            var expected = new CalledByEntryCondition();

            var actual = new CalledByEntryCondition();

            var notEqual = new CalledByContractCondition
            {
                Hash = UInt160.Parse("0xfff4f52ca43d6bf4fec8647a60415b183303d961"),
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual<WitnessCondition>(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_CalledByContractCondition()
        {
            var expected = new CalledByContractCondition
            {
                Hash = UInt160.Zero,
            };

            var actual = new CalledByContractCondition
            {
                Hash = UInt160.Zero,
            };

            var notEqual = new CalledByContractCondition
            {
                Hash = UInt160.Parse("0xfff4f52ca43d6bf4fec8647a60415b183303d961"),
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_BooleanCondition()
        {
            var expected = new BooleanCondition
            {
                Expression = true,
            };

            var actual = new BooleanCondition
            {
                Expression = true,
            };

            var notEqual = new BooleanCondition
            {
                Expression = false,
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_AndCondition()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var hash = UInt160.Zero;
            var expected = new AndCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                    new CalledByGroupCondition { Group = point }
                }
            };

            var actual = new AndCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                    new CalledByGroupCondition { Group = point }
                }
            };

            var notEqual = new AndCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                }
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void Test_IEquatable_OrCondition()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var hash = UInt160.Zero;
            var expected = new OrCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                    new CalledByGroupCondition { Group = point }
                }
            };

            var actual = new OrCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                    new CalledByGroupCondition { Group = point }
                }
            };

            var notEqual = new OrCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                }
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }

        [TestMethod]
        public void TestFromJson1()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var hash = UInt160.Zero;
            var condition = new OrCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new CalledByContractCondition { Hash = hash },
                    new CalledByGroupCondition { Group = point }
                }
            };
            var json = condition.ToJson();
            var new_condi = WitnessCondition.FromJson(json, 2);
            Assert.IsTrue(new_condi is OrCondition);
            var or_condi = (OrCondition)new_condi;
            Assert.AreEqual(2, or_condi.Expressions.Length);
            Assert.IsTrue(or_condi.Expressions[0] is CalledByContractCondition);
            var cbcc = (CalledByContractCondition)(or_condi.Expressions[0]);
            Assert.IsTrue(or_condi.Expressions[1] is CalledByGroupCondition);
            var cbgc = (CalledByGroupCondition)(or_condi.Expressions[1]);
            Assert.IsTrue(cbcc.Hash.Equals(hash));
            Assert.IsTrue(cbgc.Group.Equals(point));
        }

        [TestMethod]
        public void TestFromJson2()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var hash1 = UInt160.Zero;
            var hash2 = UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf");
            var jstr = "{\"type\":\"Or\",\"expressions\":[{\"type\":\"And\",\"expressions\":[{\"type\":\"CalledByContract\",\"hash\":\"0x0000000000000000000000000000000000000000\"},{\"type\":\"ScriptHash\",\"hash\":\"0xd2a4cff31913016155e38e474a2c06d08be276cf\"}]},{\"type\":\"Or\",\"expressions\":[{\"type\":\"CalledByGroup\",\"group\":\"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c\"},{\"type\":\"Boolean\",\"expression\":true}]}]}";
            var json = (JObject)JToken.Parse(jstr);
            var condi = WitnessCondition.FromJson(json, WitnessCondition.MaxNestingDepth);
            var or_condi = (OrCondition)condi;
            Assert.AreEqual(2, or_condi.Expressions.Length);
            var and_condi = (AndCondition)or_condi.Expressions[0];
            var or_condi1 = (OrCondition)or_condi.Expressions[1];
            Assert.AreEqual(2, and_condi.Expressions.Length);
            Assert.AreEqual(2, or_condi1.Expressions.Length);
            var cbcc = (CalledByContractCondition)and_condi.Expressions[0];
            var cbsc = (ScriptHashCondition)and_condi.Expressions[1];
            Assert.IsTrue(cbcc.Hash.Equals(hash1));
            Assert.IsTrue(cbsc.Hash.Equals(hash2));
            var cbgc = (CalledByGroupCondition)or_condi1.Expressions[0];
            var bc = (BooleanCondition)or_condi1.Expressions[1];
            Assert.IsTrue(cbgc.Group.Equals(point));
            Assert.IsTrue(bc.Expression);
        }

        [TestMethod]
        public void Test_WitnessCondition_Nesting()
        {
            WitnessCondition nested;

            nested = new OrCondition
            {
                Expressions = new WitnessCondition[]
                {
                    new OrCondition
            {
            Expressions = new WitnessCondition[]
            {
                new BooleanCondition { Expression = true }
            }
            }
                }
            };

            var buf = nested.ToArray();
            var reader = new MemoryReader(buf);

            var deser = WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(nested, deser);

            nested = new AndCondition
            {
                Expressions = new WitnessCondition[]
                    {
                    new AndCondition
            {
            Expressions = new WitnessCondition[]
            {
                new BooleanCondition { Expression = true }
            }
            }
                    }
            };

            buf = nested.ToArray();
            reader = new MemoryReader(buf);

            deser = WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(nested, deser);

            nested = new NotCondition
            {
                Expression = new NotCondition
                {
                    Expression = new BooleanCondition { Expression = true }
                }
            };

            buf = nested.ToArray();
            reader = new MemoryReader(buf);

            deser = WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(nested, deser);

            // Overflow maxNestingDepth
            nested = new OrCondition
            {
                Expressions = new WitnessCondition[]
                    {
                    new OrCondition
            {
            Expressions = new WitnessCondition[] {
                new OrCondition
                {
                Expressions = new WitnessCondition[]
                {
                    new BooleanCondition { Expression = true }
                }
                }
            }
            }
                    }
            };

            buf = nested.ToArray();
            reader = new MemoryReader(buf);

            var exceptionHappened = false;
            // CS8175 prevents from using Assert.ThrowsException here
            try
            {
                WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            }
            catch (FormatException)
            {
                exceptionHappened = true;
            }
            Assert.IsTrue(exceptionHappened);

            nested = new AndCondition
            {
                Expressions = new WitnessCondition[]
                    {
                    new AndCondition
            {
            Expressions = new WitnessCondition[] {
                new AndCondition
                {
                Expressions = new WitnessCondition[]
                {
                    new BooleanCondition { Expression = true }
                }
                }
            }
            }
                    }
            };

            buf = nested.ToArray();
            reader = new MemoryReader(buf);

            exceptionHappened = false;
            // CS8175 prevents from using Assert.ThrowsException here
            try
            {
                WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            }
            catch (FormatException)
            {
                exceptionHappened = true;
            }
            Assert.IsTrue(exceptionHappened);

            nested = new NotCondition
            {
                Expression = new NotCondition
                {
                    Expression = new NotCondition
                    {
                        Expression = new BooleanCondition { Expression = true }
                    }
                }
            };

            buf = nested.ToArray();
            reader = new MemoryReader(buf);

            exceptionHappened = false;
            // CS8175 prevents from using Assert.ThrowsException here
            try
            {
                WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            }
            catch (FormatException)
            {
                exceptionHappened = true;
            }
            Assert.IsTrue(exceptionHappened);
        }
    }
}
