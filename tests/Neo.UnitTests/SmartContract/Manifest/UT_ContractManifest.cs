// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractManifest.cs file belongs to the neo project and is free
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
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractManifest
    {
        [TestMethod]
        public void TestMainnetContract()
        {
            // 0x6f1837723768f27a6f6a14452977e3e0e264f2cc in mainnet
            // read json from TestFile/SampleContract.manifest.json
            var path = Path.Combine("SmartContract", "Manifest", "TestFile", "SampleContract.manifest.json");
            var json = File.ReadAllText(path);
            var manifest = ContractManifest.Parse(json);

            var counter = new ReferenceCounter();
            var item = manifest.ToStackItem(counter);
            var data = BinarySerializer.Serialize(item, 1024 * 1024, 4096);

            Assert.ThrowsExactly<FormatException>(() => _ = BinarySerializer.Deserialize(data, ExecutionEngineLimits.Default, counter));
            Assert.ThrowsExactly<FormatException>(() => _ = BinarySerializer.Serialize(item, 1024 * 1024, 2048));

            item = BinarySerializer.Deserialize(data, ExecutionEngineLimits.Default with { MaxStackSize = 4096 }, counter);
            var copy = item.ToInteroperable<ContractManifest>();

            Assert.AreEqual(manifest.ToJson().ToString(false), copy.ToJson().ToString(false));
        }

        [TestMethod]
        public void ParseFromJson_Default()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            var json = """
            {
                "name": "testManifest",
                "groups": [],
                "features": {},
                "supportedstandards": [],
                "abi": {
                    "methods": [
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions": [{"contract":"*","methods":"*"}],
                "trusts": [],
                "extra": null
            }
            """;

            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache);
            Assert.AreEqual(manifest.ToJson().ToString(), json);
            Assert.AreEqual(manifest.ToJson().ToString(), TestUtils.CreateDefaultManifest().ToJson().ToString());
            Assert.IsTrue(manifest.IsValid(engine, UInt160.Zero));
            Assert.IsFalse(manifest.Abi.HasNEP25);
        }

        [TestMethod]
        public void ParseFromJson_Permissions()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[
                    {"contract":"0x0000000000000000000000000000000000000000","methods":["method1","method2"]}
                ],
                "trusts": [],
                "extra": null
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Permissions = [
                new ContractPermission()
                {
                    Contract = ContractPermissionDescriptor.Create(UInt160.Zero),
                    Methods = WildcardContainer<string>.Create("method1", "method2")
                }
            ];
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void EqualTests()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[
                    {"contract":"0x0000000000000000000000000000000000000000","methods":["method1","method2"]}
                ],
                "trusts":[],
                "extra":null
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifestA = ContractManifest.Parse(json);
            var manifestB = ContractManifest.Parse(json);

            Assert.IsTrue(manifestA.Abi.Methods.SequenceEqual(manifestB.Abi.Methods));

            for (int x = 0; x < manifestA.Abi.Methods.Length; x++)
            {
                Assert.IsTrue(manifestA.Abi.Methods[x] == manifestB.Abi.Methods[x]);
                Assert.IsFalse(manifestA.Abi.Methods[x] != manifestB.Abi.Methods[x]);
            }
        }

        [TestMethod]
        public void ParseFromJson_SafeMethods()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[
                    {"contract":"*","methods":"*"}
                ],
                "trusts":[],
                "extra": null
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_Trust()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[
                    {"contract":"*","methods":"*"}
                ],
                "trusts":["0x0000000000000000000000000000000000000001", "*"],
                "extra":null
            }
            """;
            json = Regex.Replace(json, @"\s+", "");

            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(
                ContractPermissionDescriptor.Create(UInt160.Parse("0x0000000000000000000000000000000000000001")),
                ContractPermissionDescriptor.CreateWildcard());
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_ExtendedTypeMismatch_ShouldThrow()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {
                            "name":"testMethod",
                            "parameters":[
                                {
                                    "name":"arg",
                                    "type":"Integer",
                                    "extendedtype":{
                                        "type":"String"
                                    }
                                }
                            ],
                            "returntype":"Void",
                            "offset":0,
                            "safe":true
                        }
                    ],
                    "events":[]
                },
                "permissions":[],
                "trusts":[],
                "extra":null
            }
            """;

            json = Regex.Replace(json, @"\s+", "");
            Assert.ThrowsExactly<FormatException>(() => ContractManifest.Parse(json));
        }

        [TestMethod]
        public void ParseFromJson_UnknownNamedType_ShouldThrow()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {
                            "name":"testMethod",
                            "parameters":[
                                {
                                    "name":"arg",
                                    "type":"Array",
                                    "extendedtype":{
                                        "type":"Array",
                                        "namedtype":"Custom.Struct"
                                    }
                                }
                            ],
                            "returntype":"Void",
                            "offset":0,
                            "safe":true
                        }
                    ],
                    "events":[]
                },
                "permissions":[],
                "trusts":[],
                "extra":null
            }
            """;

            json = Regex.Replace(json, @"\s+", "");
            Assert.ThrowsExactly<FormatException>(() => ContractManifest.Parse(json));
        }

        [TestMethod]
        public void ToInteroperable_Trust()
        {
            var json = """
            {
                "name":"CallOracleContract-6",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[{
                        "name":"request",
                        "parameters":[
                            {"name":"url","type":"String"},
                            {"name":"filter","type":"String"},
                            {"name":"gasForResponse","type":"Integer"}
                        ],
                        "returntype":"Void",
                        "offset":0,
                        "safe":false
                    },{
                        "name":"callback",
                        "parameters":[
                            {"name":"url","type":"String"},
                            {"name":"userData","type":"Any"},
                            {"name":"responseCode","type":"Integer"},
                            {"name":"response","type":"ByteArray"}
                        ],
                        "returntype":"Void",
                        "offset":86,
                        "safe":false
                    },{
                        "name":"getStoredUrl",
                        "parameters":[],
                        "returntype":"String",
                        "offset":129,
                        "safe":false
                    },{
                        "name":"getStoredResponseCode",
                        "parameters":[],
                        "returntype":"Integer",
                        "offset":142,
                        "safe":false
                    },{
                        "name":"getStoredResponse",
                        "parameters":[],
                        "returntype":"ByteArray",
                        "offset":165,
                        "safe":false
                    }],
                    "events":[]
                },
                "permissions":[
                    {"contract":"0xfe924b7cfe89ddd271abaf7210a80a7e11178758","methods":"*"},
                    {"contract":"*","methods":"*"}
                ],
                "trusts":["0xfe924b7cfe89ddd271abaf7210a80a7e11178758", "*"],
                "extra":{}
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);
            var s = (Struct)manifest.ToStackItem(new ReferenceCounter());
            manifest = s.ToInteroperable<ContractManifest>();

            Assert.IsFalse(manifest.Permissions[0].Contract.IsWildcard);
            Assert.IsTrue(manifest.Permissions[0].Methods.IsWildcard);
            Assert.IsTrue(manifest.Permissions[1].Contract.IsWildcard);
            Assert.IsTrue(manifest.Permissions[1].Methods.IsWildcard);

            Assert.IsFalse(manifest.Trusts[0].IsWildcard);
            Assert.IsTrue(manifest.Trusts[1].IsWildcard);
        }

        [TestMethod]
        public void ParseFromJson_Groups()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[{
                    "pubkey":"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                    "signature":"QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQQ=="
                }],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[
                    {"contract":"*","methods":"*"}
                ],
                "trusts":[],
                "extra":null
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var signature = string.Concat(Enumerable.Repeat("41", 64));
            var check = TestUtils.CreateDefaultManifest();
            check.Groups = [
                new ContractGroup() {
                    PubKey = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1),
                    Signature = signature.HexToBytes()
                }
            ];
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_Extra()
        {
            var json = """
            {
                "name":"testManifest",
                "groups":[],
                "features":{},
                "supportedstandards":[],
                "abi":{
                    "methods":[
                        {"name":"testMethod","parameters":[],"returntype":"Void","offset":0,"safe":true}
                    ],
                    "events":[]
                },
                "permissions":[{"contract":"*","methods":"*"}],
                "trusts":[],
                "extra":{"key":"value"}
            }
            """;
            json = Regex.Replace(json, @"\s+", "");
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(json, manifest.ToJson().ToString());
            Assert.AreEqual("value", manifest.Extra["key"].AsString(), false);
        }

        [TestMethod]
        public void TestDeserializeAndSerialize()
        {
            var expected = TestUtils.CreateDefaultManifest();
            expected.Extra = (JObject)JToken.Parse(@"{""a"":123}");

            var clone = (ContractManifest)RuntimeHelpers.GetUninitializedObject(typeof(ContractManifest));
            ((IInteroperable)clone).FromStackItem(expected.ToStackItem(null));

            Assert.AreEqual(@"{""a"":123}", expected.Extra.ToString());
            Assert.AreEqual(expected.ToString(), clone.ToString());

            expected.Extra = null;
            clone = (ContractManifest)RuntimeHelpers.GetUninitializedObject(typeof(ContractManifest));
            ((IInteroperable)clone).FromStackItem(expected.ToStackItem(null));

            Assert.AreEqual(expected.Extra, clone.Extra);
            Assert.AreEqual(expected.ToString(), clone.ToString());
        }

        [TestMethod]
        public void TestSerializeTrusts()
        {
            var check = TestUtils.CreateDefaultManifest();
            check.Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(
                ContractPermissionDescriptor.Create(UInt160.Parse("0x0000000000000000000000000000000000000001")),
                ContractPermissionDescriptor.CreateWildcard());
            var si = check.ToStackItem(null);

            var actualTrusts = ((Array)si)[6];

            Assert.HasCount(2, (Array)actualTrusts);
            Assert.AreEqual(((Array)actualTrusts)[0], new ByteString(UInt160.Parse("0x0000000000000000000000000000000000000001").ToArray()));
            // Wildcard trust should be represented as Null stackitem (not as zero-length ByteString):
            Assert.AreEqual(((Array)actualTrusts)[1], StackItem.Null);
        }
    }
}
