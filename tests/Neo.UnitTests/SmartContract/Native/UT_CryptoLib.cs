using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BLS12_381;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

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
            "8123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
                .ToLower().HexToBytes();

        [TestMethod]
        public void TestG1()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<G1Affine>().ToCompressed().ToHexString().Should().Be("97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb");
        }

        [TestMethod]
        public void TestG2()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g2);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<G2Affine>().ToCompressed().ToHexString().Should().Be("93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8");
        }

        [TestMethod]
        public void TestNotG1()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", not_g1);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }

        [TestMethod]
        public void TestNotG2()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", not_g2);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }
        [TestMethod]
        public void TestBls12381Add()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", gt);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Add");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
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
            var snapshot = TestBlockchain.GetTestSnapshot();
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

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
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

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                var result = engine.ResultStack.Pop();
                result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("014E367F06F92BB039AEDCDD4DF65FC05A0D985B4CA6B79AA2254A6C605EB424048FA7F6117B8D4DA8522CD9C767B0450EEF9FA162E25BD305F36D77D8FEDE115C807C0805968129F15C1AD8489C32C41CB49418B4AEF52390900720B6D8B02C0EAB6A8B1420007A88412AB65DE0D04FEECCA0302E7806761483410365B5E771FCE7E5431230AD5E9E1C280E8953C68D0BD06236E9BD188437ADC14D42728C6E7177399B6B5908687F491F91EE6CCA3A391EF6C098CBEAEE83D962FA604A718A0C9DB625A7AAC25034517EB8743B5868A3803B37B94374E35F152F922BA423FB8E9B3D2B2BBF9DD602558CA5237D37420502B03D12B9230ED2A431D807B81BD18671EBF78380DD3CF490506187996E7C72F53C3914C76342A38A536FFAED478318CDD273F0D38833C07467EAF77743B70C924D43975D3821D47110A358757F926FCF970660FBDD74EF15D93B81E3AA290C78F59CBC6ED0C1E0DCBADFD11A73EB7137850D29EFEB6FA321330D0CF70F5C7F6B004BCF86AC99125F8FECF83157930BEC2AF89F8B378C6D7F63B0A07B3651F5207A84F62CEE929D574DA154EBE795D519B661086F069C9F061BA3B53DC4910EA1614C87B114E2F9EF328AC94E93D00440B412D5AE5A3C396D52D26C0CDF2156EBD3D3F60EA500C42120A7CE1F7EF80F15323118956B17C09E80E96ED4E1572461D604CDE2533330C684F86680406B1D3EE830CBAFE6D29C9A0A2F41E03E26095B713EB7E782144DB1EC6B53047FCB606B7B665B3DD1F52E95FCF2AE59C4AB159C3F98468C0A43C36C022B548189B6".ToLower());
            }
        }

        [TestMethod]
        public void TestBls12381Pairing()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g2);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Pairing");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetInterface<Gt>().ToArray().ToHexString().Should().Be("0F41E58663BF08CF068672CBD01A7EC73BACA4D72CA93544DEFF686BFD6DF543D48EAA24AFE47E1EFDE449383B67663104C581234D086A9902249B64728FFD21A189E87935A954051C7CDBA7B3872629A4FAFC05066245CB9108F0242D0FE3EF03350F55A7AEFCD3C31B4FCB6CE5771CC6A0E9786AB5973320C806AD360829107BA810C5A09FFDD9BE2291A0C25A99A211B8B424CD48BF38FCEF68083B0B0EC5C81A93B330EE1A677D0D15FF7B984E8978EF48881E32FAC91B93B47333E2BA5706FBA23EB7C5AF0D9F80940CA771B6FFD5857BAAF222EB95A7D2809D61BFE02E1BFD1B68FF02F0B8102AE1C2D5D5AB1A19F26337D205FB469CD6BD15C3D5A04DC88784FBB3D0B2DBDEA54D43B2B73F2CBB12D58386A8703E0F948226E47EE89D018107154F25A764BD3C79937A45B84546DA634B8F6BE14A8061E55CCEBA478B23F7DACAA35C8CA78BEAE9624045B4B601B2F522473D171391125BA84DC4007CFBF2F8DA752F7C74185203FCCA589AC719C34DFFBBAAD8431DAD1C1FB597AAA5193502B86EDB8857C273FA075A50512937E0794E1E65A7617C90D8BD66065B1FFFE51D7A579973B1315021EC3C19934F1368BB445C7C2D209703F239689CE34C0378A68E72A6B3B216DA0E22A5031B54DDFF57309396B38C881C4C849EC23E87089A1C5B46E5110B86750EC6A532348868A84045483C92B7AF5AF689452EAFABF1A8943E50439F1D59882A98EAA0170F1250EBD871FC0A92A7B2D83168D0D727272D441BEFA15C503DD8E90CE98DB3E7B6D194F60839C508A84305AACA1789B6".ToLower());
        }

        [TestMethod]
        public void Bls12381Equal()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", g1);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.All);
            script.EmitPush("bls12381Equal");
            script.EmitPush(NativeContract.CryptoLib.Hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = engine.ResultStack.Pop();
            result.GetBoolean().Should().BeTrue();
        }
    }
}
