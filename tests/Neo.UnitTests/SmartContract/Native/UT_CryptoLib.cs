// Copyright (C) 2015-2024 The Neo Project.
//
// UT_CryptoLib.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_CryptoLib
    {
        private readonly byte[] g1 = "97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb".ToLower().HexToBytes();
        private readonly byte[] g2 = "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8".ToLower().HexToBytes();
        private readonly byte[] gt = "0f41e58663bf08cf068672cbd01a7ec73baca4d72ca93544deff686bfd6df543d48eaa24afe47e1efde449383b67663104c581234d086a9902249b64728ffd21a189e87935a954051c7cdba7b3872629a4fafc05066245cb9108f0242d0fe3ef03350f55a7aefcd3c31b4fcb6ce5771cc6a0e9786ab5973320c806ad360829107ba810c5a09ffdd9be2291a0c25a99a211b8b424cd48bf38fcef68083b0b0ec5c81a93b330ee1a677d0d15ff7b984e8978ef48881e32fac91b93b47333e2ba5706fba23eb7c5af0d9f80940ca771b6ffd5857baaf222eb95a7d2809d61bfe02e1bfd1b68ff02f0b8102ae1c2d5d5ab1a19f26337d205fb469cd6bd15c3d5a04dc88784fbb3d0b2dbdea54d43b2b73f2cbb12d58386a8703e0f948226e47ee89d018107154f25a764bd3c79937a45b84546da634b8f6be14a8061e55cceba478b23f7dacaa35c8ca78beae9624045b4b601b2f522473d171391125ba84dc4007cfbf2f8da752f7c74185203fcca589ac719c34dffbbaad8431dad1c1fb597aaa5193502b86edb8857c273fa075a50512937e0794e1e65a7617c90d8bd66065b1fffe51d7a579973b1315021ec3c19934f1368bb445c7c2d209703f239689ce34c0378a68e72a6b3b216da0e22a5031b54ddff57309396b38c881c4c849ec23e87089a1c5b46e5110b86750ec6a532348868a84045483c92b7af5af689452eafabf1a8943e50439f1d59882a98eaa0170f1250ebd871fc0a92a7b2d83168d0d727272d441befa15c503dd8e90ce98db3e7b6d194f60839c508a84305aaca1789b6".ToLower().HexToBytes();


        private readonly byte[] not_g1 =
            "8123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef".ToLower().HexToBytes();
        private readonly byte[] not_g2 =
            "8123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef".ToLower().HexToBytes();

        [TestMethod]
        public void TestG1()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<G1Affine>().ToCompressed().ToHexString().Should().Be("97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb");
        }

        [TestMethod]
        public void TestG2()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g2);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<G2Affine>().ToCompressed().ToHexString().Should().Be("93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8");
        }

        [TestMethod]
        public void TestNotG1()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", not_g1);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }

        [TestMethod]
        public void TestNotG2()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", not_g2);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }
        [TestMethod]
        public void TestBls12381Add()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Add");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("079AB7B345EB23C944C957A36A6B74C37537163D4CBF73BAD9751DE1DD9C68EF72CB21447E259880F72A871C3EDA1B0C017F1C95CF79B22B459599EA57E613E00CB75E35DE1F837814A93B443C54241015AC9761F8FB20A44512FF5CFC04AC7F0F6B8B52B2B5D0661CBF232820A257B8C5594309C01C2A45E64C6A7142301E4FB36E6E16B5A85BD2E437599D103C3ACE06D8046C6B3424C4CD2D72CE98D279F2290A28A87E8664CB0040580D0C485F34DF45267F8C215DCBCD862787AB555C7E113286DEE21C9C63A458898BEB35914DC8DAAAC453441E7114B21AF7B5F47D559879D477CF2A9CBD5B40C86BECD071280900410BB2751D0A6AF0FE175DCF9D864ECAAC463C6218745B543F9E06289922434EE446030923A3E4C4473B4E3B1914081ABD33A78D31EB8D4C1BB3BAAB0529BB7BAF1103D848B4CEAD1A8E0AA7A7B260FBE79C67DBE41CA4D65BA8A54A72B61692A61CE5F4D7A093B2C46AA4BCA6C4A66CF873D405EBC9C35D8AA639763720177B23BEFFAF522D5E41D3C5310EA3331409CEBEF9EF393AA00F2AC64673675521E8FC8FDDAF90976E607E62A740AC59C3DDDF95A6DE4FBA15BEB30C43D4E3F803A3734DBEB064BF4BC4A03F945A4921E49D04AB8D45FD753A28B8FA082616B4B17BBCB685E455FF3BF8F60C3BD32A0C185EF728CF41A1B7B700B7E445F0B372BC29E370BC227D443C70AE9DBCF73FEE8ACEDBD317A286A53266562D817269C004FB0F149DD925D2C590A960936763E519C2B62E14C7759F96672CD852194325904197B0B19C6B528AB33566946AF39B".ToLower());
        }

        [TestMethod]
        public void TestBls12381Mul()
        {
            var data = new byte[32];
            data[0] = 0x03;
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using (ScriptBuilder script = new())
            {
                script.EmitPush(false);
                script.EmitPush(data);
                script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
                script.EmitPush(3);
                script.Emit(OpCode.PACK);
                script.EmitPush(CallFlags.All);
                script.EmitPush("bls12381Mul");
                script.EmitPush(NativeContract.CryptoLib.Hash);
                script.EmitSysCall(ApplicationEngine.System_Contract_Call);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                var result = engine.ResultStack.Pop();
                result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("18B2DB6B3286BAEA116CCAD8F5554D170A69B329A6DE5B24C50B8834965242001A1C58089FD872B211ACD3263897FA660B117248D69D8AC745283A3E6A4CCEC607F6CF7CEDEE919575D4B7C8AE14C36001F76BE5FCA50ADC296EF8DF4926FA7F0B55A75F255FE61FC2DA7CFFE56ADC8775AAAB54C50D0C4952AD919D90FB0EB221C41ABB9F2352A11BE2D7F176ABE41E0E30AFB34FC2CE16136DE66900D92068F30011E9882C0A56E7E7B30F08442BE9E58D093E1888151136259D059FB539210D635BC491D5244A16CA28FDCF10546EC0F7104D3A419DDC081BA30ECB0CD2289010C2D385946229B7A9735ADC82736914FE61AD26C6C38B787775DE3B939105DE055F8D7004358272A0823F6F1787A7ABB6C3C59C8C9CBD1674AC900512632818CDD273F0D38833C07467EAF77743B70C924D43975D3821D47110A358757F926FCF970660FBDD74EF15D93B81E3AA290C78F59CBC6ED0C1E0DCBADFD11A73EB7137850D29EFEB6FA321330D0CF70F5C7F6B004BCF86AC99125F8FECF83157930BEC2AF89F8B378C6D7F63B0A07B3651F5207A84F62CEE929D574DA154EBE795D519B661086F069C9F061BA3B53DC4910EA1614C87B114E2F9EF328AC94E93D00440B412D5AE5A3C396D52D26C0CDF2156EBD3D3F60EA500C42120A7CE1F7EF80F15323118956B17C09E80E96ED4E1572461D604CDE2533330C684F86680406B1D3EE830CBAFE6D29C9A0A2F41E03E26095B713EB7E782144DB1EC6B53047FCB606B7B665B3DD1F52E95FCF2AE59C4AB159C3F98468C0A43C36C022B548189B6".ToLower());
            }
            using (ScriptBuilder script = new())
            {
                script.EmitPush(true);
                script.EmitPush(data);
                script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
                script.EmitPush(3);
                script.Emit(OpCode.PACK);
                script.EmitPush(CallFlags.All);
                script.EmitPush("bls12381Mul");
                script.EmitPush(NativeContract.CryptoLib.Hash);
                script.EmitSysCall(ApplicationEngine.System_Contract_Call);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                var result = engine.ResultStack.Pop();
                result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("014E367F06F92BB039AEDCDD4DF65FC05A0D985B4CA6B79AA2254A6C605EB424048FA7F6117B8D4DA8522CD9C767B0450EEF9FA162E25BD305F36D77D8FEDE115C807C0805968129F15C1AD8489C32C41CB49418B4AEF52390900720B6D8B02C0EAB6A8B1420007A88412AB65DE0D04FEECCA0302E7806761483410365B5E771FCE7E5431230AD5E9E1C280E8953C68D0BD06236E9BD188437ADC14D42728C6E7177399B6B5908687F491F91EE6CCA3A391EF6C098CBEAEE83D962FA604A718A0C9DB625A7AAC25034517EB8743B5868A3803B37B94374E35F152F922BA423FB8E9B3D2B2BBF9DD602558CA5237D37420502B03D12B9230ED2A431D807B81BD18671EBF78380DD3CF490506187996E7C72F53C3914C76342A38A536FFAED478318CDD273F0D38833C07467EAF77743B70C924D43975D3821D47110A358757F926FCF970660FBDD74EF15D93B81E3AA290C78F59CBC6ED0C1E0DCBADFD11A73EB7137850D29EFEB6FA321330D0CF70F5C7F6B004BCF86AC99125F8FECF83157930BEC2AF89F8B378C6D7F63B0A07B3651F5207A84F62CEE929D574DA154EBE795D519B661086F069C9F061BA3B53DC4910EA1614C87B114E2F9EF328AC94E93D00440B412D5AE5A3C396D52D26C0CDF2156EBD3D3F60EA500C42120A7CE1F7EF80F15323118956B17C09E80E96ED4E1572461D604CDE2533330C684F86680406B1D3EE830CBAFE6D29C9A0A2F41E03E26095B713EB7E782144DB1EC6B53047FCB606B7B665B3DD1F52E95FCF2AE59C4AB159C3F98468C0A43C36C022B548189B6".ToLower());
            }
        }

        [TestMethod]
        public void TestBls12381Pairing()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g2);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Pairing");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("0F41E58663BF08CF068672CBD01A7EC73BACA4D72CA93544DEFF686BFD6DF543D48EAA24AFE47E1EFDE449383B67663104C581234D086A9902249B64728FFD21A189E87935A954051C7CDBA7B3872629A4FAFC05066245CB9108F0242D0FE3EF03350F55A7AEFCD3C31B4FCB6CE5771CC6A0E9786AB5973320C806AD360829107BA810C5A09FFDD9BE2291A0C25A99A211B8B424CD48BF38FCEF68083B0B0EC5C81A93B330EE1A677D0D15FF7B984E8978EF48881E32FAC91B93B47333E2BA5706FBA23EB7C5AF0D9F80940CA771B6FFD5857BAAF222EB95A7D2809D61BFE02E1BFD1B68FF02F0B8102AE1C2D5D5AB1A19F26337D205FB469CD6BD15C3D5A04DC88784FBB3D0B2DBDEA54D43B2B73F2CBB12D58386A8703E0F948226E47EE89D018107154F25A764BD3C79937A45B84546DA634B8F6BE14A8061E55CCEBA478B23F7DACAA35C8CA78BEAE9624045B4B601B2F522473D171391125BA84DC4007CFBF2F8DA752F7C74185203FCCA589AC719C34DFFBBAAD8431DAD1C1FB597AAA5193502B86EDB8857C273FA075A50512937E0794E1E65A7617C90D8BD66065B1FFFE51D7A579973B1315021EC3C19934F1368BB445C7C2D209703F239689CE34C0378A68E72A6B3B216DA0E22A5031B54DDFF57309396B38C881C4C849EC23E87089A1C5B46E5110B86750EC6A532348868A84045483C92B7AF5AF689452EAFABF1A8943E50439F1D59882A98EAA0170F1250EBD871FC0A92A7B2D83168D0D727272D441BEFA15C503DD8E90CE98DB3E7B6D194F60839C508A84305AACA1789B6".ToLower());
        }

        [TestMethod]
        public void Bls12381Equal()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Equal");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetBoolean().Should().BeTrue();
        }

        private enum BLS12381PointType : byte
        {
            G1Proj,
            G2Proj,
            GT
        }

        private void CheckBls12381ScalarMul_Compat(string point, string mul, bool negative, string expected, BLS12381PointType expectedType)
        {
            var data = new byte[32];
            data[0] = 0x03;
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using (ScriptBuilder script = new())
            {
                script.EmitPush(negative);
                script.EmitPush(mul.ToLower().HexToBytes());
                script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", point.ToLower().HexToBytes());
                script.EmitPush(3);
                script.Emit(OpCode.PACK);
                script.EmitPush(CallFlags.All);
                script.EmitPush("bls12381Mul");
                script.EmitPush(NativeContract.CryptoLib.Hash);
                script.EmitSysCall(ApplicationEngine.System_Contract_Call);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                var result = engine.ResultStack.Pop();
                switch (expectedType)
                {
                    case BLS12381PointType.G1Proj:
                        {
                            new G1Affine(result.GetInterface<G1Projective>()).ToCompressed().ToHexString().Should().Be(expected);
                            break;
                        }
                    case BLS12381PointType.G2Proj:
                        {
                            new G2Affine(result.GetInterface<G2Projective>()).ToCompressed().ToHexString().Should().Be(expected);
                            break;
                        }
                    case BLS12381PointType.GT:
                        {
                            result.GetInterface<Gt>().ToArray().ToHexString().Should().Be(expected);
                            break;
                        }
                    default:
                        Assert.Fail("Unknown result point type.");
                        break;
                }
            }
        }

        [TestMethod]
        public void TestBls12381ScalarMul_Compat()
        {
            // GT mul by positive scalar.
            CheckBls12381ScalarMul_Compat(
                "14fd52fe9bfd08bbe23fcdf1d3bc5390c62e75a8786a72f8a343123a30a7c5f8d18508a21a2bf902f4db2c068913bc1c130e7ce13260d601c89ee717acfd3d4e1d80f409dd2a5c38b176f0b64d3d0a224c502717270dfecf2b825ac24608215c0d7fcfdf3c1552ada42b7e0521bc2e7389436660c352ecbf2eedf30b77b6b501df302399e6240473af47abe56fc974780c214542fcc0cf10e3001fa5e82d398f6ba1ddd1ccdf133bfd75e033eae50aec66bd5e884b8c74d4c1c6ac7c01278ac5164a54600cb2e24fec168f82542fbf98234dbb9ddf06503dc3c497da88b73db584ba19e685b1b398b51f40160e6c8f0917b4a68dedcc04674e5f5739cf0d845ba801263f712ed4ddda59c1d9909148e3f28124ae770682c9b19233bf0bcfa00d05bfe708d381b066b83a883ba8251ce2ea6772cbde51e1322d82b2c8a026a2153f4822e20cb69b8b05003ee74e09cb481728d688caa8a671f90b55488e272f48c7c5ae32526d3635a5343eb02640358d9ac445c76a5d8f52f653bbaee04ba5ce03c68b88c25be6fd3611cc21c9968e4f87e541beeccc5170b8696a439bb666ad8a6608ab30ebc7dfe56eaf0dd9ab8439171a6e4e0d608e6e6c8ac5ddcf8d6d2a950d06051e6b6c4d3feb6dc8dac2acadd345cadfb890454a2101a112f7471f0e001701f60f3d4352c4d388c0f198854908c0e939719709c1b3f82d2a25cc7156a3838bc141e041c259849326fbd0839f15cea6a78b89349dcd1c03695a74e72d3657af4ee2cf267337bc96363ef4a1c5d5d7a673cc3a3c1a1350043f99537d62",
                "8463159bd9a1d1e1fd815172177ec24c0c291353ed88b3d1838fd9d63b1efd0b",
                false,
                "03dc980ce0c037634816f9fc1edb2e1807e38a51f838e3a684f195d6c52c41d6a8a5b64d57d3fda507bebe3bd4b661af0e4f7c46754b373c955982b4d64a24838cbc010d04b6ceb499bf411d114dab77eaf70f96ab66c2868dcd63706b602b07010c487fc16c90b61e1c2ad33c31c8f3fc86d114a59b127ac584640f149f3597102c55dd1ed8a305a10c052c0a724e570fc079e410123735a6144ccd88d9e4e91d7b889f80b18a1741eacd6f244fce3cf57795e619b6648b9238053b4b8e4ed6115c905fbcb61525370667ff43144e12b700662a7344ac1af97f11d09779ca6865973f95ff318b42ff00df7c6eb958160947a0ab6cb25534af51ce1f0b076907c6eb5ce0760bd7670cab8814cc3308766eb6e52b5427dbf85d6424990fd3354515ab880358bc55075a08f36b855694c02ee0bd63adefe235ba4ee41dc600a1cae950c1dc760bf7b1edd8712e9e90eebb19de705e29f4feb870129441bd4b9e91c3d37e60c12fa79a5b1e4132ba9498044e6fbf2de37e4dd88b4e9095b46f122019e73a561ba3967b32813c3ec74b8e1b6ab619eeab698e6638114cb29ca9c3d353192db3d392fee2b4dfdfd36b13db440534dd754417cffcd470f4d4cfdcb6d7896181c27b8b30622d7a4ca0a05a7ea67ca011cab07738235b115bbd330239691487d2de5d679a8cad2fe5c7fff16b0b0f3f929619c8005289c3d7ffe5bcd5ea19651bfc9366682a2790cab45ee9a98815bb7e58dc666e2209cd9d700546cf181ceb43fe719243930984b696b0d18d4cd1f5d960e149a2b753b1396e4f8f3b16",
                BLS12381PointType.GT
            );
            // GT mul by positive scalar.
            CheckBls12381ScalarMul_Compat(
                "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8",
                "8463159bd9a1d1e1fd815172177ec24c0c291353ed88b3d1838fd9d63b1efd0b",
                false,
                "88ae9bba988e854877c66dfb7ff84aa5e107861aa51d1a2a8dac2414d716a7e219bc4b0239e4b12d2182f57b5eea82830639f2e6713098ae8d4b4c3942f366614bac35c91c83ecb57fa90fe03094aca1ecd3555a7a6fdfa2417b5bb06917732e",
                BLS12381PointType.G2Proj
            );
            // GT mul by negative scalar.
            CheckBls12381ScalarMul_Compat(
                "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8",
                "8463159bd9a1d1e1fd815172177ec24c0c291353ed88b3d1838fd9d63b1efd0b",
                true,
                "a8ae9bba988e854877c66dfb7ff84aa5e107861aa51d1a2a8dac2414d716a7e219bc4b0239e4b12d2182f57b5eea82830639f2e6713098ae8d4b4c3942f366614bac35c91c83ecb57fa90fe03094aca1ecd3555a7a6fdfa2417b5bb06917732e",
                BLS12381PointType.G2Proj
            );
            // GT mul by zero scalar.
            CheckBls12381ScalarMul_Compat(
                "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8",
                "0000000000000000000000000000000000000000000000000000000000000000",
                false,
                "c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
                BLS12381PointType.G2Proj
            );
            // G1Affine mul by positive scalar.
            CheckBls12381ScalarMul_Compat(
                "a1f9855f7670a63e4c80d64dfe6ddedc2ed2bfaebae27e4da82d71ba474987a39808e8921d3df97df6e5d4b979234de8",
                "8463159bd9a1d1e1fd815172177ec24c0c291353ed88b3d1838fd9d63b1efd0b",
                false,
                "ae85e3e2d677c9e3424ed79b5a7554262c3d6849202b84d2e7024e4b1f2e9dd3f7cf20b807a9f2a67d87e47e9e94d361",
                BLS12381PointType.G1Proj
            );
            // G1Affine mul by negative scalar.
            CheckBls12381ScalarMul_Compat(
                "a1f9855f7670a63e4c80d64dfe6ddedc2ed2bfaebae27e4da82d71ba474987a39808e8921d3df97df6e5d4b979234de8",
                "8463159bd9a1d1e1fd815172177ec24c0c291353ed88b3d1838fd9d63b1efd0b",
                true,
                "8e85e3e2d677c9e3424ed79b5a7554262c3d6849202b84d2e7024e4b1f2e9dd3f7cf20b807a9f2a67d87e47e9e94d361",
                BLS12381PointType.G1Proj
            );
            // G1Affine mul by zero scalar.
            CheckBls12381ScalarMul_Compat(
                "a1f9855f7670a63e4c80d64dfe6ddedc2ed2bfaebae27e4da82d71ba474987a39808e8921d3df97df6e5d4b979234de8",
                "0000000000000000000000000000000000000000000000000000000000000000",
                false,
                "c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
                BLS12381PointType.G1Proj
            );
            // G2Affine mul by positive scalar.
            CheckBls12381ScalarMul_Compat(
                "a41e586fdd58d39616fea921a855e65417a5732809afc35e28466e3acaeed3d53dd4b97ca398b2f29bf6bbcaca026a6609a42bdeaaeef42813ae225e35c23c61c293e6ecb6759048fb76ac648ba3bc49f0fcf62f73fca38cdc5e7fa5bf511365",
                "cbfffe3e37e53e31306addde1a1725641fbe88cd047ee7477966c44a3f764b47",
                false,
                "88ae9bba988e854877c66dfb7ff84aa5e107861aa51d1a2a8dac2414d716a7e219bc4b0239e4b12d2182f57b5eea82830639f2e6713098ae8d4b4c3942f366614bac35c91c83ecb57fa90fe03094aca1ecd3555a7a6fdfa2417b5bb06917732e",
                BLS12381PointType.G2Proj
            );
            // G2Affine mul by negative scalar.
            CheckBls12381ScalarMul_Compat(
                "a41e586fdd58d39616fea921a855e65417a5732809afc35e28466e3acaeed3d53dd4b97ca398b2f29bf6bbcaca026a6609a42bdeaaeef42813ae225e35c23c61c293e6ecb6759048fb76ac648ba3bc49f0fcf62f73fca38cdc5e7fa5bf511365",
                "cbfffe3e37e53e31306addde1a1725641fbe88cd047ee7477966c44a3f764b47",
                true,
                "a8ae9bba988e854877c66dfb7ff84aa5e107861aa51d1a2a8dac2414d716a7e219bc4b0239e4b12d2182f57b5eea82830639f2e6713098ae8d4b4c3942f366614bac35c91c83ecb57fa90fe03094aca1ecd3555a7a6fdfa2417b5bb06917732e",
                BLS12381PointType.G2Proj
            );
            // G2Affine mul by negative scalar.
            CheckBls12381ScalarMul_Compat(
                "a41e586fdd58d39616fea921a855e65417a5732809afc35e28466e3acaeed3d53dd4b97ca398b2f29bf6bbcaca026a6609a42bdeaaeef42813ae225e35c23c61c293e6ecb6759048fb76ac648ba3bc49f0fcf62f73fca38cdc5e7fa5bf511365",
                "0000000000000000000000000000000000000000000000000000000000000000",
                false,
                "c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
                BLS12381PointType.G2Proj
            );
        }

        /// <summary>
        /// Keccak256 cases are verified in https://emn178.github.io/online-tools/keccak_256.html
        /// </summary>
        [TestMethod]
        public void TestKeccak256_HelloWorld()
        {
            // Arrange
            byte[] inputData = "Hello, World!"u8.ToArray();
            string expectedHashHex = "acaf3289d7b601cbd114fb36c4d29c85bbfd5e133f14cb355c3fd8d99367964f";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for 'Hello, World!'.");
        }
        [TestMethod]
        public void TestKeccak256_Keccak()
        {
            // Arrange
            byte[] inputData = "Keccak"u8.ToArray();
            string expectedHashHex = "868c016b666c7d3698636ee1bd023f3f065621514ab61bf26f062c175fdbe7f2";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for 'Keccak'.");
        }

        [TestMethod]
        public void TestKeccak256_Cryptography()
        {
            // Arrange
            byte[] inputData = "Cryptography"u8.ToArray();
            string expectedHashHex = "53d49d225dd2cfe77d8c5e2112bcc9efe77bea1c7aa5e5ede5798a36e99e2d29";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for 'Cryptography'.");
        }

        [TestMethod]
        public void TestKeccak256_Testing123()
        {
            // Arrange
            byte[] inputData = "Testing123"u8.ToArray();
            string expectedHashHex = "3f82db7b16b0818a1c6b2c6152e265f682d5ebcf497c9aad776ad38bc39cb6ca";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for 'Testing123'.");
        }

        [TestMethod]
        public void TestKeccak256_LongString()
        {
            // Arrange
            byte[] inputData = "This is a longer string for Keccak256 testing purposes."u8.ToArray();
            string expectedHashHex = "24115e5c2359f85f6840b42acd2f7ea47bc239583e576d766fa173bf711bdd2f";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for the longer string.");
        }

        [TestMethod]
        public void TestKeccak256_BlankString()
        {
            // Arrange
            byte[] inputData = ""u8.ToArray();
            string expectedHashHex = "c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470";

            // Act
            byte[] outputData = CryptoLib.Keccak256(inputData);
            string outputHashHex = Hex.ToHexString(outputData);

            // Assert
            Assert.AreEqual(expectedHashHex, outputHashHex, "Keccak256 hash did not match expected value for blank string.");
        }

        // TestVerifyWithECDsa_CustomTxWitness_SingleSig builds custom witness verification script for single Koblitz public key
        // and ensures witness check is passed for the following message:
        //
        //	keccak256([4-bytes-network-magic-LE, txHash-bytes-BE])
        //
        // The proposed witness verification script has 110 bytes length, verification costs 2154270  * 10e-8GAS including Invocation script execution.
        // The user has to sign the keccak256([4-bytes-network-magic-LE, txHash-bytes-BE]).
        [TestMethod]
        public void TestVerifyWithECDsa_CustomTxWitness_SingleSig()
        {
            byte[] privkey = "7177f0d04c79fa0b8c91fe90c1cf1d44772d1fba6e5eb9b281a22cd3aafb51fe".HexToBytes();
            ECPoint pubKey = ECPoint.Parse("04fd0a8c1ce5ae5570fdd46e7599c16b175bf0ebdfe9c178f1ab848fb16dac74a5d301b0534c7bcf1b3760881f0c420d17084907edd771e1c9c8e941bbf6ff9108", ECCurve.Secp256k1);

            // vrf is a builder of witness verification script corresponding to the public key.
            using ScriptBuilder vrf = new();
            vrf.EmitPush((byte)NamedCurveHash.secp256k1Keccak256); // push Koblitz curve identifier and Keccak256 hasher.
            vrf.Emit(OpCode.SWAP); // swap curve identifier with the signature.
            vrf.EmitPush(pubKey.EncodePoint(true)); // emit the caller's public key.

            // Construct and push the signed message. The signed message is effectively the network-dependent transaction hash,
            // i.e. msg = [4-network-magic-bytes-LE, tx-hash-BE]
            // Firstly, retrieve network magic (it's uint32 wrapped into BigInteger and represented as Integer stackitem on stack).
            vrf.EmitSysCall(ApplicationEngine.System_Runtime_GetNetwork); // push network magic (Integer stackitem), can have 0-5 bytes length serialized.

            // Convert network magic to 4-bytes-length LE byte array representation.
            vrf.EmitPush(0x100000000); // push 0x100000000.
            vrf.Emit(OpCode.ADD, // the result is some new number that is 5 bytes at least when serialized, but first 4 bytes are intact network value (LE).
                    OpCode.PUSH4, OpCode.LEFT); // cut the first 4 bytes out of a number that is at least 5 bytes long, the result is 4-bytes-length LE network representation.

            // Retrieve executing transaction hash.
            vrf.EmitSysCall(ApplicationEngine.System_Runtime_GetScriptContainer); // push the script container (executing transaction, actually).
            vrf.Emit(OpCode.PUSH0, OpCode.PICKITEM); // pick 0-th transaction item (the transaction hash).

            // Concatenate network magic and transaction hash.
            vrf.Emit(OpCode.CAT); // this instruction will convert network magic to bytes using BigInteger rules of conversion.

            // Continue construction of 'verifyWithECDsa' call.
            vrf.Emit(OpCode.PUSH4, OpCode.PACK); // pack arguments for 'verifyWithECDsa' call.
            EmitAppCallNoArgs(vrf, CryptoLib.CryptoLib.Hash, "verifyWithECDsa", CallFlags.None); // emit the call to 'verifyWithECDsa' itself.

            // Account is a hash of verification script.
            var vrfScript = vrf.ToArray();
            var acc = vrfScript.ToScriptHash();

            var tx = new Transaction
            {
                Attributes = [],
                NetworkFee = 1_0000_0000,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[Transaction.MaxTransactionSize / 100],
                Signers = [new Signer { Account = acc }],
                SystemFee = 0,
                ValidUntilBlock = 10,
                Version = 0,
                Witnesses = []
            };
            var tx_signature = Crypto.Sign(tx.GetSignData(TestBlockchain.TheNeoSystem.Settings.Network), privkey, ECCurve.Secp256k1, Hasher.Keccak256);

            // inv is a builder of witness invocation script corresponding to the public key.
            using ScriptBuilder inv = new();
            inv.EmitPush(tx_signature); // push signature.

            tx.Witnesses =
            [
                new Witness { InvocationScript = inv.ToArray(), VerificationScript = vrfScript }
            ];

            tx.VerifyStateIndependent(TestProtocolSettings.Default).Should().Be(VerifyResult.Succeed);

            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            // Create fake balance to pay the fees.
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            _ = NativeContract.GAS.Mint(engine, acc, 5_0000_0000, false);
            snapshotCache.Commit();

            var txVrfContext = new TransactionVerificationContext();
            var conflicts = new List<Transaction>();
            tx.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, txVrfContext, conflicts).Should().Be(VerifyResult.Succeed);

            // The resulting witness verification cost is 2154270   * 10e-8GAS.
            // The resulting witness Invocation script (66 bytes length):
            // NEO-VM > loadbase64 DEARoaaEjM/3VulrBDUod7eiZgWQS2iXIM0+I24iyJYmffhosZoQjfnnRymF/7+FaBPb9qvQwxLLSVo9ROlrdFdC
            // READY: loaded 66 instructions
            // NEO-VM 0 > ops
            // INDEX    OPCODE       PARAMETER
            // 0        PUSHDATA1    11a1a6848ccff756e96b04352877b7a26605904b689720cd3e236e22c896267df868b19a108df9e7472985ffbf856813dbf6abd0c312cb495a3d44e96b745742    <<
            //
            //
            // The resulting witness verificaiton script (110 bytes):
            // NEO-VM 0 > loadbase64 ABhQDCEC/QqMHOWuVXD91G51mcFrF1vw69/pwXjxq4SPsW2sdKVBxfug4AMAAAAAAQAAAJ4UjUEtUQgwEM6LFMAfDA92ZXJpZnlXaXRoRUNEc2EMFBv1dasRiWiEE2EKNaEohs3gtmxyQWJ9W1I=
            // READY: loaded 110 instructions
            // NEO-VM 0 > pos
            // Error: No help topic for 'pos'
            // NEO-VM 0 > ops
            // INDEX    OPCODE       PARAMETER
            // 0        PUSHINT8     122 (7a)    <<
            // 2        SWAP
            // 3        PUSHDATA1    02fd0a8c1ce5ae5570fdd46e7599c16b175bf0ebdfe9c178f1ab848fb16dac74a5
            // 38       SYSCALL      System.Runtime.GetNetwork (c5fba0e0)
            // 43       PUSHINT64    4294967296 (0000000001000000)
            // 52       ADD
            // 53       PUSH4
            // 54       LEFT
            // 55       SYSCALL      System.Runtime.GetScriptContainer (2d510830)
            // 60       PUSH0
            // 61       PICKITEM
            // 62       CAT
            // 63       PUSH4
            // 64       PACK
            // 65       PUSH0
            // 66       PUSHDATA1    766572696679576974684543447361 ("verifyWithECDsa")
            // 83       PUSHDATA1    1bf575ab1189688413610a35a12886cde0b66c72 ("NNToUmdQBe5n8o53BTzjTFAnSEcpouyy3B", "0x726cb6e0cd8628a1350a611384688911ab75f51b")
            // 105      SYSCALL      System.Contract.Call (627d5b52)
        }

        // TestVerifyWithECDsa_CustomTxWitness_MultiSig builds custom multisignature witness verification script for Koblitz public keys
        // and ensures witness check is passed for the M out of N multisignature of message:
        //
        //	keccak256([4-bytes-network-magic-LE, txHash-bytes-BE])
        //
        // The proposed witness verification script has 264 bytes length, verification costs 8390070  * 10e-8GAS including Invocation script execution.
        // The users have to sign the keccak256([4-bytes-network-magic-LE, txHash-bytes-BE]).
        [TestMethod]
        public void TestVerifyWithECDsa_CustomTxWitness_MultiSig()
        {
            var privkey1 = "b2dde592bfce654ef03f1ceea452d2b0112e90f9f52099bcd86697a2bd0a2b60".HexToBytes();
            var pubKey1 = ECPoint.Parse("040486468683c112125978ffe876245b2006bfe739aca8539b67335079262cb27ad0dedc9e5583f99b61c6f46bf80b97eaec3654b87add0e5bd7106c69922a229d", ECCurve.Secp256k1);
            var privkey2 = "b9879e26941872ee6c9e6f01045681496d8170ed2cc4a54ce617b39ae1891b3a".HexToBytes();
            var pubKey2 = ECPoint.Parse("040d26fc2ad3b1aae20f040b5f83380670f8ef5c2b2ac921ba3bdd79fd0af0525177715fd4370b1012ddd10579698d186ab342c223da3e884ece9cab9b6638c7bb", ECCurve.Secp256k1);
            var privkey3 = "4e1fe2561a6da01ee030589d504d62b23c26bfd56c5e07dfc9b8b74e4602832a".HexToBytes();
            var pubKey3 = ECPoint.Parse("047b4e72ae854b6a0955b3e02d92651ab7fa641a936066776ad438f95bb674a269a63ff98544691663d91a6cfcd215831f01bfb7a226363a6c5c67ef14541dba07", ECCurve.Secp256k1);
            var privkey4 = "6dfd066bb989d3786043aa5c1f0476215d6f5c44f5fc3392dd15e2599b67a728".HexToBytes();
            var pubKey4 = ECPoint.Parse("04b62ac4c8a352a892feceb18d7e2e3a62c8c1ecbaae5523d89d747b0219276e225be2556a137e0e806e4915762d816cdb43f572730d23bb1b1cba750011c4edc6", ECCurve.Secp256k1);

            // Public keys must be sorted, exactly like for standard CreateMultiSigRedeemScript.
            var keys = new List<(byte[], ECPoint)>
            {
                (privkey1, pubKey1),
                (privkey2, pubKey2),
                (privkey3, pubKey3),
                (privkey4, pubKey4),
            }.OrderBy(k => k.Item2).ToList();

            // Consider 4 users willing to sign 3/4 multisignature transaction with their Secp256k1 private keys.
            var m = 3;
            var n = keys.Count;

            // Must ensure the following conditions are met before verification script construction:
            n.Should().BeGreaterThan(0);
            m.Should().BeLessThanOrEqualTo(n);
            keys.Select(k => k.Item2).Distinct().Count().Should().Be(n);

            // In fact, the following algorithm is implemented via NeoVM instructions:
            //
            // func Check(sigs []interop.Signature) bool {
            // 	if m != len(sigs) {
            // 		return false
            // 	}
            // 	var pubs []interop.PublicKey = []interop.PublicKey{...}
            // 	msg := append(convert.ToBytes(runtime.GetNetwork()), runtime.GetScriptContainer().Hash...)
            // 	var sigCnt = 0
            // 	var pubCnt = 0
            // 	for ; sigCnt < m && pubCnt < n; { // sigs must be sorted by pub
            // 		sigCnt += crypto.VerifyWithECDsa(msg, pubs[pubCnt], sigs[sigCnt], crypto.Secp256k1Keccak256)
            // 		pubCnt++
            // 	}
            // 	return sigCnt == m
            // }

            // vrf is a builder of M out of N multisig witness verification script corresponding to the public keys.
            using ScriptBuilder vrf = new();

            // Start the same way as regular multisig script.
            vrf.EmitPush(m); // push m.
            foreach (var tuple in keys)
            {
                vrf.EmitPush(tuple.Item2.EncodePoint(true)); // push public keys in compressed form.
            }
            vrf.EmitPush(n); // push n.

            // Initialize slots for local variables. Locals slot scheme:
            // LOC0 -> sigs
            // LOC1 -> pubs
            // LOC2 -> msg (ByteString)
            // LOC3 -> sigCnt (Integer)
            // LOC4 -> pubCnt (Integer)
            // LOC5 -> n
            // LOC6 -> m
            vrf.Emit(OpCode.INITSLOT, new ReadOnlySpan<byte>([7, 0])); // 7 locals, no args.

            // Store n.
            vrf.Emit(OpCode.STLOC5);

            // Pack public keys and store at LOC1.
            vrf.Emit(OpCode.LDLOC5, // load n.
                OpCode.PACK, OpCode.STLOC1); // pack pubs and store.

            // Store m.
            vrf.Emit(OpCode.STLOC6);

            // Check the number of signatures is m. Abort the execution if not.
            vrf.Emit(OpCode.DEPTH); // push the number of signatures onto stack.
            vrf.Emit(OpCode.LDLOC6); // load m.
            vrf.Emit(OpCode.JMPEQ, new ReadOnlySpan<byte>([0])); // here and below short jumps are sufficient. Offset will be filled later.
            var sigsLenCheckEndOffset = vrf.Length;
            vrf.Emit(OpCode.ABORT); // abort the execution if length of the signatures not equal to m.

            // Start the verification itself.
            var checkStartOffset = vrf.Length;

            // Pack signatures and store at LOC0.
            vrf.Emit(OpCode.LDLOC6); // load m.
            vrf.Emit(OpCode.PACK, OpCode.STLOC0);

            // Get message and store it at LOC2.
            // msg = [4-network-magic-bytes-LE, tx-hash-BE]
            vrf.EmitSysCall(ApplicationEngine.System_Runtime_GetNetwork); // push network magic (Integer stackitem), can have 0-5 bytes length serialized.
            // Convert network magic to 4-bytes-length LE byte array representation.
            vrf.EmitPush(0x100000000); // push 0x100000000.
            vrf.Emit(OpCode.ADD, // the result is some new number that is 5 bytes at least when serialized, but first 4 bytes are intact network value (LE).
                    OpCode.PUSH4, OpCode.LEFT); // cut the first 4 bytes out of a number that is at least 5 bytes long, the result is 4-bytes-length LE network representation.
            // Retrieve executing transaction hash.
            vrf.EmitSysCall(ApplicationEngine.System_Runtime_GetScriptContainer); // push the script container (executing transaction, actually).
            vrf.Emit(OpCode.PUSH0, OpCode.PICKITEM); // pick 0-th transaction item (the transaction hash).
            // Concatenate network magic and transaction hash.
            vrf.Emit(OpCode.CAT); // this instruction will convert network magic to bytes using BigInteger rules of conversion.
            vrf.Emit(OpCode.STLOC2); // store msg as a local variable #2.

            // Initialize local variables: sigCnt, pubCnt.
            vrf.Emit(OpCode.PUSH0, OpCode.STLOC3, // initialize sigCnt.
            OpCode.PUSH0, OpCode.STLOC4); // initialize pubCnt.

            // Loop condition check.
            var loopStartOffset = vrf.Length;
            vrf.Emit(OpCode.LDLOC3); // load sigCnt.
            vrf.Emit(OpCode.LDLOC6); // load m.
            vrf.Emit(OpCode.GE,     // sigCnt >= m
            OpCode.LDLOC4); // load pubCnt
            vrf.Emit(OpCode.LDLOC5);      // load n.
            vrf.Emit(OpCode.GE, // pubCnt >= n
            OpCode.OR); // sigCnt >= m || pubCnt >= n
            vrf.Emit(OpCode.JMPIF, new ReadOnlySpan<byte>([0])); // jump to the end of the script if (sigCnt >= m || pubCnt >= n).
            var loopConditionOffset = vrf.Length;

            // Loop start. Prepare arguments and call CryptoLib's verifyWithECDsa.
            vrf.EmitPush((byte)NamedCurveHash.secp256k1Keccak256); // push Koblitz curve identifier and Keccak256 hasher.
            vrf.Emit(OpCode.LDLOC0,                // load signatures.
                OpCode.LDLOC3,             // load sigCnt.
                OpCode.PICKITEM,           // pick signature at index sigCnt.
                OpCode.LDLOC1,             // load pubs.
                OpCode.LDLOC4,             // load pubCnt.
                OpCode.PICKITEM,           // pick pub at index pubCnt.
                OpCode.LDLOC2,             // load msg.
                OpCode.PUSH4, OpCode.PACK); // pack 4 arguments for 'verifyWithECDsa' call.
            EmitAppCallNoArgs(vrf, CryptoLib.CryptoLib.Hash, "verifyWithECDsa", CallFlags.None); // emit the call to 'verifyWithECDsa' itself.

            // Update loop variables.
            vrf.Emit(OpCode.LDLOC3, OpCode.ADD, OpCode.STLOC3, // increment sigCnt if signature is valid.
            OpCode.LDLOC4, OpCode.INC, OpCode.STLOC4); // increment pubCnt.

            // End of the loop.
            vrf.Emit(OpCode.JMP, new ReadOnlySpan<byte>([0])); // jump to the start of cycle.
            var loopEndOffset = vrf.Length;
            // Return condition: the number of valid signatures should be equal to m.
            var progRetOffset = vrf.Length;
            vrf.Emit(OpCode.LDLOC3);  // load sigCnt.
            vrf.Emit(OpCode.LDLOC6);      // load m.
            vrf.Emit(OpCode.NUMEQUAL); // push m == sigCnt.

            var vrfScript = vrf.ToArray();

            // Set JMP* instructions offsets. "-1" is for short JMP parameter offset. JMP parameters
            // are relative offsets.
            vrfScript[sigsLenCheckEndOffset - 1] = (byte)(checkStartOffset - sigsLenCheckEndOffset + 2);
            vrfScript[loopEndOffset - 1] = (byte)(loopStartOffset - loopEndOffset + 2);
            vrfScript[loopConditionOffset - 1] = (byte)(progRetOffset - loopConditionOffset + 2);

            // Account is a hash of verification script.
            var acc = vrfScript.ToScriptHash();

            var tx = new Transaction
            {
                Attributes = [],
                NetworkFee = 1_0000_0000,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[Transaction.MaxTransactionSize / 100],
                Signers = [new Signer { Account = acc }],
                SystemFee = 0,
                ValidUntilBlock = 10,
                Version = 0,
                Witnesses = []
            };
            // inv is a builder of witness invocation script corresponding to the public key.
            using ScriptBuilder inv = new();
            for (var i = 0; i < n; i++)
            {
                if (i == 1) // Skip one key since we need only 3 signatures.
                    continue;
                var sig = Crypto.Sign(tx.GetSignData(TestBlockchain.TheNeoSystem.Settings.Network), keys[i].Item1, ECCurve.Secp256k1, Hasher.Keccak256);
                inv.EmitPush(sig);
            }

            tx.Witnesses =
            [
                new Witness { InvocationScript = inv.ToArray(), VerificationScript = vrfScript }
            ];

            tx.VerifyStateIndependent(TestProtocolSettings.Default).Should().Be(VerifyResult.Succeed);

            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            // Create fake balance to pay the fees.
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            _ = NativeContract.GAS.Mint(engine, acc, 5_0000_0000, false);

            // We should not use commit here cause once its committed, the value we get from the snapshot can be different
            // from the underline storage. Thought there isn't any issue triggered here, its wrong to use it this way.
            // We should either ignore the commit, or get a new snapshot of the store after the commit.
            // snapshot.Commit();

            // Check that witness verification passes.
            var txVrfContext = new TransactionVerificationContext();
            var conflicts = new List<Transaction>();
            tx.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, txVrfContext, conflicts).Should().Be(VerifyResult.Succeed);

            // The resulting witness verification cost for 3/4 multisig is 8389470  * 10e-8GAS. Cost depends on M/N.
            // The resulting witness Invocation script (198 bytes for 3 signatures):
            // NEO-VM 0 > loadbase64 DEDM23XByPvDK9XRAHRhfGH7/Mp5jdaci3/GpTZ3D9SZx2Zw89tAaOtmQSIutXbCxRQA1kSeUD4AteJGoNXFhFzIDECgeHoey0rYdlFyTVfDJSsuS+VwzC5OtYGCVR2V/MttmLXWA/FWZH/MjmU0obgQXa9zoBxqYQUUJKefivZFxVcTDEAZT6L6ZFybeXbm8+RlVNS7KshusT54d2ImQ6vFvxETphhJOwcQ0yNL6qJKsrLAKAnzicY4az3ct0G35mI17/gQ
            // READY: loaded 198 instructions
            // NEO-VM 0 > ops
            // INDEX    OPCODE       PARAMETER
            // 0        PUSHDATA1    ccdb75c1c8fbc32bd5d10074617c61fbfcca798dd69c8b7fc6a536770fd499c76670f3db4068eb6641222eb576c2c51400d6449e503e00b5e246a0d5c5845cc8    <<
            // 66       PUSHDATA1    a0787a1ecb4ad87651724d57c3252b2e4be570cc2e4eb58182551d95fccb6d98b5d603f156647fcc8e6534a1b8105daf73a01c6a61051424a79f8af645c55713
            // 132      PUSHDATA1    194fa2fa645c9b7976e6f3e46554d4bb2ac86eb13e7877622643abc5bf1113a618493b0710d3234beaa24ab2b2c02809f389c6386b3ddcb741b7e66235eff810
            //
            //
            // Resulting witness verification script (266 bytes for 3/4 multisig):
            // NEO-VM 0 > loadbase64 EwwhAwSGRoaDwRISWXj/6HYkWyAGv+c5rKhTm2czUHkmLLJ6DCEDDSb8KtOxquIPBAtfgzgGcPjvXCsqySG6O915/QrwUlEMIQN7TnKuhUtqCVWz4C2SZRq3+mQak2Bmd2rUOPlbtnSiaQwhArYqxMijUqiS/s6xjX4uOmLIwey6rlUj2J10ewIZJ24iFFcHAHVtwHF2Q24oAzhuwHBBxfug4AMAAAAAAQAAAJ4UjUEtUQgwEM6LchBzEHRrbrhsbbiSJEIAGGhrzmlszmoUwB8MD3ZlcmlmeVdpdGhFQ0RzYQwUG/V1qxGJaIQTYQo1oSiGzeC2bHJBYn1bUmuec2ycdCK5a26z
            // READY: loaded 264 instructions
            // NEO-VM 0 > ops
            // INDEX    OPCODE       PARAMETER
            // 0        PUSH3            <<
            // 1        PUSHDATA1    030486468683c112125978ffe876245b2006bfe739aca8539b67335079262cb27a
            // 36       PUSHDATA1    030d26fc2ad3b1aae20f040b5f83380670f8ef5c2b2ac921ba3bdd79fd0af05251
            // 71       PUSHDATA1    037b4e72ae854b6a0955b3e02d92651ab7fa641a936066776ad438f95bb674a269
            // 106      PUSHDATA1    02b62ac4c8a352a892feceb18d7e2e3a62c8c1ecbaae5523d89d747b0219276e22
            // 141      PUSH4
            // 142      INITSLOT     7 local, 0 arg
            // 145      STLOC5
            // 146      LDLOC5
            // 147      PACK
            // 148      STLOC1
            // 149      STLOC6
            // 150      DEPTH
            // 151      LDLOC6
            // 152      JMPEQ        155 (3/03)
            // 154      ABORT
            // 155      LDLOC6
            // 156      PACK
            // 157      STLOC0
            // 158      SYSCALL      System.Runtime.GetNetwork (c5fba0e0)
            // 163      PUSHINT64    4294967296 (0000000001000000)
            // 172      ADD
            // 173      PUSH4
            // 174      LEFT
            // 175      SYSCALL      System.Runtime.GetScriptContainer (2d510830)
            // 180      PUSH0
            // 181      PICKITEM
            // 182      CAT
            // 183      STLOC2
            // 184      PUSH0
            // 185      STLOC3
            // 186      PUSH0
            // 187      STLOC4
            // 188      LDLOC3
            // 189      LDLOC6
            // 190      GE
            // 191      LDLOC4
            // 192      LDLOC5
            // 193      GE
            // 194      OR
            // 195      JMPIF        261 (66/42)
            // 197      PUSHINT8     122 (7a)
            // 199      LDLOC0
            // 200      LDLOC3
            // 201      PICKITEM
            // 202      LDLOC1
            // 203      LDLOC4
            // 204      PICKITEM
            // 205      LDLOC2
            // 206      PUSH4
            // 207      PACK
            // 208      PUSH0
            // 209      PUSHDATA1    766572696679576974684543447361 ("verifyWithECDsa")
            // 226      PUSHDATA1    1bf575ab1189688413610a35a12886cde0b66c72 ("NNToUmdQBe5n8o53BTzjTFAnSEcpouyy3B", "0x726cb6e0cd8628a1350a611384688911ab75f51b")
            // 248      SYSCALL      System.Contract.Call (627d5b52)
            // 253      LDLOC3
            // 254      ADD
            // 255      STLOC3
            // 256      LDLOC4
            // 257      INC
            // 258      STLOC4
            // 259      JMP          188 (-71/b9)
            // 261      LDLOC3
            // 262      LDLOC6
            // 263      NUMEQUAL
        }

        // EmitAppCallNoArgs is a helper method that emits all parameters of System.Contract.Call interop
        // except the method arguments.
        private static ScriptBuilder EmitAppCallNoArgs(ScriptBuilder builder, UInt160 contractHash, string method, CallFlags f)
        {
            builder.EmitPush((byte)f);
            builder.EmitPush(method);
            builder.EmitPush(contractHash);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
            return builder;
        }

        [TestMethod]
        public void TestVerifyWithECDsa()
        {
            byte[] privR1 = "6e63fda41e9e3aba9bb5696d58a75731f044a9bdc48fe546da571543b2fa460e".HexToBytes();
            ECPoint pubR1 = ECPoint.Parse("04cae768e1cf58d50260cab808da8d6d83d5d3ab91eac41cdce577ce5862d736413643bdecd6d21c3b66f122ab080f9219204b10aa8bbceb86c1896974768648f3", ECCurve.Secp256r1);
            byte[] privK1 = "0b5fb3a050385196b327be7d86cbce6e40a04c8832445af83ad19c82103b3ed9".HexToBytes();
            ECPoint pubK1 = ECPoint.Parse("04b6363b353c3ee1620c5af58594458aa00abf43a6d134d7c4cb2d901dc0f474fd74c94740bd7169aa0b1ef7bc657e824b1d7f4283c547e7ec18c8576acf84418a", ECCurve.Secp256k1);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");

            // secp256r1 + SHA256
            byte[] signature = Crypto.Sign(message, privR1, ECCurve.Secp256r1, Hasher.SHA256);
            Crypto.VerifySignature(message, signature, pubR1).Should().BeTrue(); // SHA256 hash is used by default.
            CallVerifyWithECDsa(message, pubR1, signature, NamedCurveHash.secp256r1SHA256).Should().Be(true);

            // secp256r1 + Keccak256
            signature = Crypto.Sign(message, privR1, ECCurve.Secp256r1, Hasher.Keccak256);
            Crypto.VerifySignature(message, signature, pubR1, Hasher.Keccak256).Should().BeTrue();
            CallVerifyWithECDsa(message, pubR1, signature, NamedCurveHash.secp256r1Keccak256).Should().Be(true);

            // secp256k1 + SHA256
            signature = Crypto.Sign(message, privK1, ECCurve.Secp256k1, Hasher.SHA256);
            Crypto.VerifySignature(message, signature, pubK1).Should().BeTrue(); // SHA256 hash is used by default.
            CallVerifyWithECDsa(message, pubK1, signature, NamedCurveHash.secp256k1SHA256).Should().Be(true);

            // secp256k1 + Keccak256
            signature = Crypto.Sign(message, privK1, ECCurve.Secp256k1, Hasher.Keccak256);
            Crypto.VerifySignature(message, signature, pubK1, Hasher.Keccak256).Should().BeTrue();
            CallVerifyWithECDsa(message, pubK1, signature, NamedCurveHash.secp256k1Keccak256).Should().Be(true);
        }

        private bool CallVerifyWithECDsa(byte[] message, ECPoint pub, byte[] signature, NamedCurveHash curveHash)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using (ScriptBuilder script = new())
            {
                script.EmitPush((int)curveHash);
                script.EmitPush(signature);
                script.EmitPush(pub.EncodePoint(true));
                script.EmitPush(message);
                script.EmitPush(4);
                script.Emit(OpCode.PACK);
                script.EmitPush(CallFlags.All);
                script.EmitPush("verifyWithECDsa");
                script.EmitPush(NativeContract.CryptoLib.Hash);
                script.EmitSysCall(ApplicationEngine.System_Contract_Call);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                return engine.ResultStack.Pop().GetBoolean();
            }
        }

        [TestMethod]
        public void TestSecp256k1RecoverWithSeparateComponents()
        {
            // Arrange
            byte[] privK1 = "0b5fb3a050385196b327be7d86cbce6e40a04c8832445af83ad19c82103b3ed9".HexToBytes();
            ECPoint pubK1 = ECPoint.Parse("04b6363b353c3ee1620c5af58594458aa00abf43a6d134d7c4cb2d901dc0f474fd74c94740bd7169aa0b1ef7bc657e824b1d7f4283c547e7ec18c8576acf84418a", ECCurve.Secp256k1);

            var message = new byte[] { 0x01, 0x02, 0x03 };

            // Test with SHA256
            // Sign the message
            var signature = Crypto.Sign(message, privK1, ECCurve.Secp256k1);
            byte[] r = signature.Take(32).ToArray();
            byte[] s = signature.Skip(32).Take(32).ToArray();
            BigInteger v = 27;

            CryptoLib.VerifyWithECDsa(message, pubK1.EncodePoint(true), signature, NamedCurveHash.secp256k1SHA256).Should().BeTrue();

            // Act & Assert for SHA256
            var recoveredKey = CryptoLib.Secp256K1Recover(message, Hasher.SHA256, r, s, v);
            recoveredKey.Should().NotBeNull();
            ECPoint.DecodePoint(recoveredKey, ECCurve.Secp256k1).Should().Be(pubK1);

            // Test with Keccak256
            var signatureKeccak = Crypto.Sign(message, privK1, ECCurve.Secp256k1, Hasher.Keccak256);
            byte[] rKeccak = signatureKeccak.Take(32).ToArray();
            byte[] sKeccak = signatureKeccak.Skip(32).Take(32).ToArray();
            BigInteger vKeccak = 27;

            CryptoLib.VerifyWithECDsa(message, pubK1.EncodePoint(true), signatureKeccak, NamedCurveHash.secp256k1Keccak256).Should().BeTrue();

            // Act & Assert for Keccak256
            var recoveredKeyKeccak = CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, rKeccak, sKeccak, vKeccak);
            recoveredKeyKeccak.Should().NotBeNull();
            ECPoint.DecodePoint(recoveredKeyKeccak, ECCurve.Secp256k1).Should().Be(pubK1);

            // Test failure cases for both hash types
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, null, s, v).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, r, null, v).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, r, s, 26).Should().BeNull();

            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, null, sKeccak, vKeccak).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, rKeccak, null, vKeccak).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, rKeccak, sKeccak, 26).Should().BeNull();
        }

        [TestMethod]
        public void TestSecp256k1RecoverWithCombinedSignature()
        {
            // Arrange
            byte[] privK1 = "0b5fb3a050385196b327be7d86cbce6e40a04c8832445af83ad19c82103b3ed9".HexToBytes();
            ECPoint pubK1 = ECPoint.Parse("04b6363b353c3ee1620c5af58594458aa00abf43a6d134d7c4cb2d901dc0f474fd74c94740bd7169aa0b1ef7bc657e824b1d7f4283c547e7ec18c8576acf84418a", ECCurve.Secp256k1);

            var message = new byte[] { 0x04, 0x05, 0x06 };

            // Test with SHA256
            // Create combined signature
            var rawSignature = Crypto.Sign(message, privK1, ECCurve.Secp256k1);
            var signature = new byte[65];
            rawSignature.CopyTo(signature, 0);

            // Calculate v: check which recovery ID (0 or 1) gives us back our public key
            byte recId = 0;
            // Try both possible v values
            signature[64] = 27; // recId = 0
            var recovered = CryptoLib.Secp256K1Recover(message, Hasher.SHA256, signature);
            if (recovered == null || !pubK1.Equals(ECPoint.DecodePoint(recovered, ECCurve.Secp256k1)))
            {
                signature[64] = 28; // recId = 1
                recId = 1;
            }

            CryptoLib.VerifyWithECDsa(message, pubK1.EncodePoint(true), rawSignature, NamedCurveHash.secp256k1SHA256).Should().BeTrue();

            // Act & Assert for SHA256
            var recoveredKey = CryptoLib.Secp256K1Recover(message, Hasher.SHA256, signature);
            recoveredKey.Should().NotBeNull();
            ECPoint.DecodePoint(recoveredKey, ECCurve.Secp256k1).Should().Be(pubK1);

            // Test with Keccak256
            var rawSignatureKeccak = Crypto.Sign(message, privK1, ECCurve.Secp256k1, Hasher.Keccak256);
            var signatureKeccak = new byte[65];
            rawSignatureKeccak.CopyTo(signatureKeccak, 0);

            // Calculate v for Keccak256
            byte recIdKeccak = 0;

            // Try both possible v values
            signatureKeccak[64] = 27; // recId = 0
            var recoveredKeccak = CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, signatureKeccak);
            if (recoveredKeccak == null || !pubK1.Equals(ECPoint.DecodePoint(recoveredKeccak, ECCurve.Secp256k1)))
            {
                signatureKeccak[64] = 28; // recId = 1
                recIdKeccak = 1;
            }

            CryptoLib.VerifyWithECDsa(message, pubK1.EncodePoint(true), rawSignatureKeccak, NamedCurveHash.secp256k1Keccak256).Should().BeTrue();

            // Act & Assert for Keccak256
            var recoveredKeyKeccak = CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, signatureKeccak);
            recoveredKeyKeccak.Should().NotBeNull();
            ECPoint.DecodePoint(recoveredKeyKeccak, ECCurve.Secp256k1).Should().Be(pubK1);

            // Test failure cases for both hash types
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, null).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, new byte[64]).Should().BeNull();

            var invalidVSignature = signature.ToArray();
            invalidVSignature[64] = 29; // Invalid v value
            CryptoLib.Secp256K1Recover(message, Hasher.SHA256, invalidVSignature).Should().BeNull();

            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, null).Should().BeNull();
            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, new byte[64]).Should().BeNull();

            var invalidVSignatureKeccak = signatureKeccak.ToArray();
            invalidVSignatureKeccak[64] = 29; // Invalid v value
            CryptoLib.Secp256K1Recover(message, Hasher.Keccak256, invalidVSignatureKeccak).Should().BeNull();
        }
    }
}
