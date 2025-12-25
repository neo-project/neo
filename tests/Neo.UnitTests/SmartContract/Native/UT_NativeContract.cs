// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NativeContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.VM;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_NativeContract
{
    private DataCache _snapshotCache = null!;
    /// <summary>
    /// _nativeStates contains a mapping from native contract name to expected native contract state
    /// constructed with all hardforks enabled and marshalled in JSON.
    /// </summary>
    private Dictionary<string, string> _nativeStates = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        _nativeStates = new Dictionary<string, string>
        {
            {"ContractManagement", """{"id":-1,"updatecounter":0,"hash":"0xfffdc93764dbaddd97c48f252a53ea4643faa3fd","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dA","checksum":3581846399},"manifest":{"name":"ContractManagement","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"deploy","parameters":[{"name":"nefFile","type":"ByteArray"},{"name":"manifest","type":"ByteArray"}],"returntype":"Array","offset":0,"safe":false},{"name":"deploy","parameters":[{"name":"nefFile","type":"ByteArray"},{"name":"manifest","type":"ByteArray"},{"name":"data","type":"Any"}],"returntype":"Array","offset":7,"safe":false},{"name":"destroy","parameters":[],"returntype":"Void","offset":14,"safe":false},{"name":"getContract","parameters":[{"name":"hash","type":"Hash160"}],"returntype":"Array","offset":21,"safe":true},{"name":"getContractById","parameters":[{"name":"id","type":"Integer"}],"returntype":"Array","offset":28,"safe":true},{"name":"getContractHashes","parameters":[],"returntype":"InteropInterface","offset":35,"safe":true},{"name":"getMinimumDeploymentFee","parameters":[],"returntype":"Integer","offset":42,"safe":true},{"name":"hasMethod","parameters":[{"name":"hash","type":"Hash160"},{"name":"method","type":"String"},{"name":"pcount","type":"Integer"}],"returntype":"Boolean","offset":49,"safe":true},{"name":"isContract","parameters":[{"name":"hash","type":"Hash160"}],"returntype":"Boolean","offset":56,"safe":true},{"name":"setMinimumDeploymentFee","parameters":[{"name":"value","type":"Integer"}],"returntype":"Void","offset":63,"safe":false},{"name":"update","parameters":[{"name":"nefFile","type":"ByteArray"},{"name":"manifest","type":"ByteArray"}],"returntype":"Void","offset":70,"safe":false},{"name":"update","parameters":[{"name":"nefFile","type":"ByteArray"},{"name":"manifest","type":"ByteArray"},{"name":"data","type":"Any"}],"returntype":"Void","offset":77,"safe":false}],"events":[{"name":"Deploy","parameters":[{"name":"Hash","type":"Hash160"}]},{"name":"Update","parameters":[{"name":"Hash","type":"Hash160"}]},{"name":"Destroy","parameters":[{"name":"Hash","type":"Hash160"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}""" },
            {"StdLib", """{"id":-2,"updatecounter":0,"hash":"0xacce6fd80d44e1796aa0c2c625e9e4e0ce39efc0","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQA==","checksum":2426471238},"manifest":{"name":"StdLib","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"atoi","parameters":[{"name":"value","type":"String"}],"returntype":"Integer","offset":0,"safe":true},{"name":"atoi","parameters":[{"name":"value","type":"String"},{"name":"base","type":"Integer"}],"returntype":"Integer","offset":7,"safe":true},{"name":"base58CheckDecode","parameters":[{"name":"s","type":"String"}],"returntype":"ByteArray","offset":14,"safe":true},{"name":"base58CheckEncode","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"String","offset":21,"safe":true},{"name":"base58Decode","parameters":[{"name":"s","type":"String"}],"returntype":"ByteArray","offset":28,"safe":true},{"name":"base58Encode","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"String","offset":35,"safe":true},{"name":"base64Decode","parameters":[{"name":"s","type":"String"}],"returntype":"ByteArray","offset":42,"safe":true},{"name":"base64Encode","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"String","offset":49,"safe":true},{"name":"base64UrlDecode","parameters":[{"name":"s","type":"String"}],"returntype":"String","offset":56,"safe":true},{"name":"base64UrlEncode","parameters":[{"name":"data","type":"String"}],"returntype":"String","offset":63,"safe":true},{"name":"deserialize","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"Any","offset":70,"safe":true},{"name":"hexDecode","parameters":[{"name":"str","type":"String"}],"returntype":"ByteArray","offset":77,"safe":true},{"name":"hexEncode","parameters":[{"name":"bytes","type":"ByteArray"}],"returntype":"String","offset":84,"safe":true},{"name":"itoa","parameters":[{"name":"value","type":"Integer"}],"returntype":"String","offset":91,"safe":true},{"name":"itoa","parameters":[{"name":"value","type":"Integer"},{"name":"base","type":"Integer"}],"returntype":"String","offset":98,"safe":true},{"name":"jsonDeserialize","parameters":[{"name":"json","type":"ByteArray"}],"returntype":"Any","offset":105,"safe":true},{"name":"jsonSerialize","parameters":[{"name":"item","type":"Any"}],"returntype":"ByteArray","offset":112,"safe":true},{"name":"memoryCompare","parameters":[{"name":"str1","type":"ByteArray"},{"name":"str2","type":"ByteArray"}],"returntype":"Integer","offset":119,"safe":true},{"name":"memorySearch","parameters":[{"name":"mem","type":"ByteArray"},{"name":"value","type":"ByteArray"}],"returntype":"Integer","offset":126,"safe":true},{"name":"memorySearch","parameters":[{"name":"mem","type":"ByteArray"},{"name":"value","type":"ByteArray"},{"name":"start","type":"Integer"}],"returntype":"Integer","offset":133,"safe":true},{"name":"memorySearch","parameters":[{"name":"mem","type":"ByteArray"},{"name":"value","type":"ByteArray"},{"name":"start","type":"Integer"},{"name":"backward","type":"Boolean"}],"returntype":"Integer","offset":140,"safe":true},{"name":"serialize","parameters":[{"name":"item","type":"Any"}],"returntype":"ByteArray","offset":147,"safe":true},{"name":"strLen","parameters":[{"name":"str","type":"String"}],"returntype":"Integer","offset":154,"safe":true},{"name":"stringSplit","parameters":[{"name":"str","type":"String"},{"name":"separator","type":"String"}],"returntype":"Array","offset":161,"safe":true},{"name":"stringSplit","parameters":[{"name":"str","type":"String"},{"name":"separator","type":"String"},{"name":"removeEmptyEntries","type":"Boolean"}],"returntype":"Array","offset":168,"safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"CryptoLib", """{"id":-3,"updatecounter":0,"hash":"0x726cb6e0cd8628a1350a611384688911ab75f51b","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQA==","checksum":174904780},"manifest":{"name":"CryptoLib","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"bls12381Add","parameters":[{"name":"x","type":"InteropInterface"},{"name":"y","type":"InteropInterface"}],"returntype":"InteropInterface","offset":0,"safe":true},{"name":"bls12381Deserialize","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"InteropInterface","offset":7,"safe":true},{"name":"bls12381Equal","parameters":[{"name":"x","type":"InteropInterface"},{"name":"y","type":"InteropInterface"}],"returntype":"Boolean","offset":14,"safe":true},{"name":"bls12381Mul","parameters":[{"name":"x","type":"InteropInterface"},{"name":"mul","type":"ByteArray"},{"name":"neg","type":"Boolean"}],"returntype":"InteropInterface","offset":21,"safe":true},{"name":"bls12381Pairing","parameters":[{"name":"g1","type":"InteropInterface"},{"name":"g2","type":"InteropInterface"}],"returntype":"InteropInterface","offset":28,"safe":true},{"name":"bls12381Serialize","parameters":[{"name":"g","type":"InteropInterface"}],"returntype":"ByteArray","offset":35,"safe":true},{"name":"keccak256","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"ByteArray","offset":42,"safe":true},{"name":"murmur32","parameters":[{"name":"data","type":"ByteArray"},{"name":"seed","type":"Integer"}],"returntype":"ByteArray","offset":49,"safe":true},{"name":"recoverSecp256K1","parameters":[{"name":"messageHash","type":"ByteArray"},{"name":"signature","type":"ByteArray"}],"returntype":"ByteArray","offset":56,"safe":true},{"name":"ripemd160","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"ByteArray","offset":63,"safe":true},{"name":"sha256","parameters":[{"name":"data","type":"ByteArray"}],"returntype":"ByteArray","offset":70,"safe":true},{"name":"verifyWithECDsa","parameters":[{"name":"message","type":"ByteArray"},{"name":"pubkey","type":"ByteArray"},{"name":"signature","type":"ByteArray"},{"name":"curveHash","type":"Integer"}],"returntype":"Boolean","offset":77,"safe":true},{"name":"verifyWithEd25519","parameters":[{"name":"message","type":"ByteArray"},{"name":"pubkey","type":"ByteArray"},{"name":"signature","type":"ByteArray"}],"returntype":"Boolean","offset":84,"safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"LedgerContract", """{"id":-4,"updatecounter":0,"hash":"0xda65b600f7124ce6c79950c1772a36403104f2be","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0A=","checksum":1110259869},"manifest":{"name":"LedgerContract","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"currentHash","parameters":[],"returntype":"Hash256","offset":0,"safe":true},{"name":"currentIndex","parameters":[],"returntype":"Integer","offset":7,"safe":true},{"name":"getBlock","parameters":[{"name":"indexOrHash","type":"ByteArray"}],"returntype":"Array","offset":14,"safe":true},{"name":"getTransaction","parameters":[{"name":"hash","type":"Hash256"}],"returntype":"Array","offset":21,"safe":true},{"name":"getTransactionFromBlock","parameters":[{"name":"blockIndexOrHash","type":"ByteArray"},{"name":"txIndex","type":"Integer"}],"returntype":"Array","offset":28,"safe":true},{"name":"getTransactionHeight","parameters":[{"name":"hash","type":"Hash256"}],"returntype":"Integer","offset":35,"safe":true},{"name":"getTransactionSigners","parameters":[{"name":"hash","type":"Hash256"}],"returntype":"Array","offset":42,"safe":true},{"name":"getTransactionVMState","parameters":[{"name":"hash","type":"Hash256"}],"returntype":"Integer","offset":49,"safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"PolicyContract", """{"id":-7,"updatecounter":0,"hash":"0xcc5e4edd9f5f8dba8bb65734541df7a1c081c67b","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dA","checksum":341349534},"manifest":{"name":"PolicyContract","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"blockAccount","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Boolean","offset":0,"safe":false},{"name":"getAttributeFee","parameters":[{"name":"attributeType","type":"Integer"}],"returntype":"Integer","offset":7,"safe":true},{"name":"getBlockedAccounts","parameters":[],"returntype":"InteropInterface","offset":14,"safe":true},{"name":"getExecFeeFactor","parameters":[],"returntype":"Integer","offset":21,"safe":true},{"name":"getFeePerByte","parameters":[],"returntype":"Integer","offset":28,"safe":true},{"name":"getStoragePrice","parameters":[],"returntype":"Integer","offset":35,"safe":true},{"name":"getWhitelistFeeContracts","parameters":[],"returntype":"InteropInterface","offset":42,"safe":true},{"name":"isBlocked","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Boolean","offset":49,"safe":true},{"name":"removeWhitelistFeeContract","parameters":[{"name":"contractHash","type":"Hash160"},{"name":"method","type":"String"},{"name":"argCount","type":"Integer"}],"returntype":"Void","offset":56,"safe":false},{"name":"setAttributeFee","parameters":[{"name":"attributeType","type":"Integer"},{"name":"value","type":"Integer"}],"returntype":"Void","offset":63,"safe":false},{"name":"setExecFeeFactor","parameters":[{"name":"value","type":"Integer"}],"returntype":"Void","offset":70,"safe":false},{"name":"setFeePerByte","parameters":[{"name":"value","type":"Integer"}],"returntype":"Void","offset":77,"safe":false},{"name":"setStoragePrice","parameters":[{"name":"value","type":"Integer"}],"returntype":"Void","offset":84,"safe":false},{"name":"setWhitelistFeeContract","parameters":[{"name":"contractHash","type":"Hash160"},{"name":"method","type":"String"},{"name":"argCount","type":"Integer"},{"name":"fixedFee","type":"Integer"}],"returntype":"Void","offset":91,"safe":false},{"name":"unblockAccount","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Boolean","offset":98,"safe":false}],"events":[{"name":"WhitelistFeeChanged","parameters":[{"name":"contract","type":"Hash160"},{"name":"method","type":"String"},{"name":"argCount","type":"Integer"},{"name":"fee","type":"Any"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"RoleManagement", """{"id":-8,"updatecounter":0,"hash":"0x49cf4e5378ffcd4dec034fd98a174c5491e395e2","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0A=","checksum":983638438},"manifest":{"name":"RoleManagement","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"designateAsRole","parameters":[{"name":"role","type":"Integer"},{"name":"nodes","type":"Array"}],"returntype":"Void","offset":0,"safe":false},{"name":"getDesignatedByRole","parameters":[{"name":"role","type":"Integer"},{"name":"index","type":"Integer"}],"returntype":"Array","offset":7,"safe":true}],"events":[{"name":"Designation","parameters":[{"name":"Role","type":"Integer"},{"name":"BlockIndex","type":"Integer"},{"name":"Old","type":"Array"},{"name":"New","type":"Array"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"OracleContract", """{"id":-9,"updatecounter":0,"hash":"0xfe924b7cfe89ddd271abaf7210a80a7e11178758","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0A=","checksum":2663858513},"manifest":{"name":"OracleContract","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"finish","parameters":[],"returntype":"Void","offset":0,"safe":false},{"name":"getPrice","parameters":[],"returntype":"Integer","offset":7,"safe":true},{"name":"request","parameters":[{"name":"url","type":"String"},{"name":"filter","type":"String"},{"name":"callback","type":"String"},{"name":"userData","type":"Any"},{"name":"gasForResponse","type":"Integer"}],"returntype":"Void","offset":14,"safe":false},{"name":"setPrice","parameters":[{"name":"price","type":"Integer"}],"returntype":"Void","offset":21,"safe":false},{"name":"verify","parameters":[],"returntype":"Boolean","offset":28,"safe":true}],"events":[{"name":"OracleRequest","parameters":[{"name":"Id","type":"Integer"},{"name":"RequestContract","type":"Hash160"},{"name":"Url","type":"String"},{"name":"Filter","type":"String"}]},{"name":"OracleResponse","parameters":[{"name":"Id","type":"Integer"},{"name":"OriginalTx","type":"Hash256"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"Notary", """{"id":-10,"updatecounter":0,"hash":"0xc1e14f19c3e60d0b9244d06dd7ba9b113135ec3b","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0A=","checksum":1110259869},"manifest":{"name":"Notary","groups":[],"features":{},"supportedstandards":["NEP-27"],"abi":{"methods":[{"name":"_onPayment","parameters":[{"name":"assetId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Void","offset":0,"safe":false},{"name":"balanceOf","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Integer","offset":7,"safe":true},{"name":"expirationOf","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Integer","offset":14,"safe":true},{"name":"getMaxNotValidBeforeDelta","parameters":[],"returntype":"Integer","offset":21,"safe":true},{"name":"lockDepositUntil","parameters":[{"name":"account","type":"Hash160"},{"name":"till","type":"Integer"}],"returntype":"Boolean","offset":28,"safe":false},{"name":"setMaxNotValidBeforeDelta","parameters":[{"name":"value","type":"Integer"}],"returntype":"Void","offset":35,"safe":false},{"name":"verify","parameters":[{"name":"signature","type":"ByteArray"}],"returntype":"Boolean","offset":42,"safe":true},{"name":"withdraw","parameters":[{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"}],"returntype":"Boolean","offset":49,"safe":false}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"Treasury", """{"id":-11,"updatecounter":0,"hash":"0x156326f25b1b5d839a4d326aeaa75383c9563ac1","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dA","checksum":1592866325},"manifest":{"name":"Treasury","groups":[],"features":{},"supportedstandards":["NEP-26","NEP-27"],"abi":{"methods":[{"name":"onNEP11Payment","parameters":[{"name":"from","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"tokenId","type":"ByteArray"},{"name":"data","type":"Any"}],"returntype":"Void","offset":0,"safe":true},{"name":"onNEP17Payment","parameters":[{"name":"from","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Void","offset":7,"safe":true},{"name":"verify","parameters":[],"returntype":"Boolean","offset":14,"safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""},
            {"TokenManagement", """{"id":-12,"updatecounter":0,"hash":"0xae00c57daeb20f9b6545f65a018f44a8a40e049f","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQA==","checksum":1841570703},"manifest":{"name":"TokenManagement","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"balanceOf","parameters":[{"name":"assetId","type":"Hash160"},{"name":"account","type":"Hash160"}],"returntype":"Integer","offset":0,"safe":true},{"name":"burn","parameters":[{"name":"assetId","type":"Hash160"},{"name":"account","type":"Hash160"},{"name":"amount","type":"Integer"}],"returntype":"Void","offset":7,"safe":false},{"name":"burnNFT","parameters":[{"name":"uniqueId","type":"Hash160"}],"returntype":"Void","offset":14,"safe":false},{"name":"create","parameters":[{"name":"name","type":"String"},{"name":"symbol","type":"String"},{"name":"decimals","type":"Integer"}],"returntype":"Hash160","offset":21,"safe":false},{"name":"create","parameters":[{"name":"name","type":"String"},{"name":"symbol","type":"String"},{"name":"decimals","type":"Integer"},{"name":"maxSupply","type":"Integer"}],"returntype":"Hash160","offset":28,"safe":false},{"name":"createNonFungible","parameters":[{"name":"name","type":"String"},{"name":"symbol","type":"String"}],"returntype":"Hash160","offset":35,"safe":false},{"name":"createNonFungible","parameters":[{"name":"name","type":"String"},{"name":"symbol","type":"String"},{"name":"maxSupply","type":"Integer"}],"returntype":"Hash160","offset":42,"safe":false},{"name":"getNFTInfo","parameters":[{"name":"uniqueId","type":"Hash160"}],"returntype":"Array","offset":49,"safe":true},{"name":"getNFTs","parameters":[{"name":"assetId","type":"Hash160"}],"returntype":"InteropInterface","offset":56,"safe":true},{"name":"getNFTsOfOwner","parameters":[{"name":"account","type":"Hash160"}],"returntype":"InteropInterface","offset":63,"safe":true},{"name":"getTokenInfo","parameters":[{"name":"assetId","type":"Hash160"}],"returntype":"Array","offset":70,"safe":true},{"name":"mint","parameters":[{"name":"assetId","type":"Hash160"},{"name":"account","type":"Hash160"},{"name":"amount","type":"Integer"}],"returntype":"Void","offset":77,"safe":false},{"name":"mintNFT","parameters":[{"name":"assetId","type":"Hash160"},{"name":"account","type":"Hash160"}],"returntype":"Hash160","offset":84,"safe":false},{"name":"mintNFT","parameters":[{"name":"assetId","type":"Hash160"},{"name":"account","type":"Hash160"},{"name":"properties","type":"Map"}],"returntype":"Hash160","offset":91,"safe":false},{"name":"transfer","parameters":[{"name":"assetId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Boolean","offset":98,"safe":false},{"name":"transferNFT","parameters":[{"name":"uniqueId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"data","type":"Any"}],"returntype":"Boolean","offset":105,"safe":false}],"events":[{"name":"Created","parameters":[{"name":"assetId","type":"Hash160"},{"name":"type","type":"Integer"}]},{"name":"Transfer","parameters":[{"name":"assetId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"amount","type":"Integer"}]},{"name":"NFTTransfer","parameters":[{"name":"uniqueId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}""" },
            {"Governance","""{"id":-13,"updatecounter":0,"hash":"0x4ce7159d05c667940413a58c25bf63063570ca67","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQA==","checksum":3261162603},"manifest":{"name":"Governance","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"_onTransfer","parameters":[{"name":"assetId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Void","offset":0,"safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}""" },
            {"NeoToken", """{"id":-14,"updatecounter":0,"hash":"0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5","nef":{"magic":860243278,"compiler":"neo-core-v3.0","source":"","tokens":[],"script":"EEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dAEEEa93tnQBBBGvd7Z0AQQRr3e2dA","checksum":1991619121},"manifest":{"name":"NeoToken","groups":[],"features":{},"supportedstandards":["NEP-17","NEP-27"],"abi":{"methods":[{"name":"_onPayment","parameters":[{"name":"assetId","type":"Hash160"},{"name":"from","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Void","offset":0,"safe":false},{"name":"balanceOf","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Integer","offset":7,"safe":true},{"name":"decimals","parameters":[],"returntype":"Integer","offset":14,"safe":true},{"name":"getAccountState","parameters":[{"name":"account","type":"Hash160"}],"returntype":"Array","offset":21,"safe":true},{"name":"getAllCandidates","parameters":[],"returntype":"InteropInterface","offset":28,"safe":true},{"name":"getCandidateVote","parameters":[{"name":"pubKey","type":"PublicKey"}],"returntype":"Integer","offset":35,"safe":true},{"name":"getCandidates","parameters":[],"returntype":"Array","offset":42,"safe":true},{"name":"getCommittee","parameters":[],"returntype":"Array","offset":49,"safe":true},{"name":"getCommitteeAddress","parameters":[],"returntype":"Hash160","offset":56,"safe":true},{"name":"getGasPerBlock","parameters":[],"returntype":"Integer","offset":63,"safe":true},{"name":"getNextBlockValidators","parameters":[],"returntype":"Array","offset":70,"safe":true},{"name":"getRegisterPrice","parameters":[],"returntype":"Integer","offset":77,"safe":true},{"name":"registerCandidate","parameters":[{"name":"pubkey","type":"PublicKey"}],"returntype":"Boolean","offset":84,"safe":false},{"name":"setGasPerBlock","parameters":[{"name":"gasPerBlock","type":"Integer"}],"returntype":"Void","offset":91,"safe":false},{"name":"setRegisterPrice","parameters":[{"name":"registerPrice","type":"Integer"}],"returntype":"Void","offset":98,"safe":false},{"name":"symbol","parameters":[],"returntype":"String","offset":105,"safe":true},{"name":"totalSupply","parameters":[],"returntype":"Integer","offset":112,"safe":true},{"name":"transfer","parameters":[{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"amount","type":"Integer"},{"name":"data","type":"Any"}],"returntype":"Boolean","offset":119,"safe":false},{"name":"unclaimedGas","parameters":[{"name":"account","type":"Hash160"},{"name":"end","type":"Integer"}],"returntype":"Integer","offset":126,"safe":true},{"name":"unregisterCandidate","parameters":[{"name":"pubkey","type":"PublicKey"}],"returntype":"Boolean","offset":133,"safe":false},{"name":"vote","parameters":[{"name":"account","type":"Hash160"},{"name":"voteTo","type":"PublicKey"}],"returntype":"Boolean","offset":140,"safe":false}],"events":[{"name":"Transfer","parameters":[{"name":"from","type":"Hash160"},{"name":"to","type":"Hash160"},{"name":"amount","type":"Integer"}]},{"name":"CandidateStateChanged","parameters":[{"name":"pubkey","type":"PublicKey"},{"name":"registered","type":"Boolean"},{"name":"votes","type":"Integer"}]},{"name":"Vote","parameters":[{"name":"account","type":"Hash160"},{"name":"from","type":"PublicKey"},{"name":"to","type":"PublicKey"},{"name":"amount","type":"Integer"}]},{"name":"CommitteeChanged","parameters":[{"name":"old","type":"Array"},{"name":"new","type":"Array"}]}]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":null}}"""}
        };
    }

    class Active : IHardforkActivable
    {
        public Hardfork? ActiveIn { get; init; }
        public Hardfork? DeprecatedIn { get; init; }
    }

    [TestMethod]
    public void TestGetContract()
    {
        Assert.AreEqual(NativeContract.GetContract(NativeContract.NEO.Hash), NativeContract.NEO);
    }

    [TestMethod]
    public void TestGenesisNEP17Manifest()
    {
        var persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 1,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = []
        };
        var snapshot = _snapshotCache.CloneCache();

        // Ensure that native NEP17 contracts contain proper supported standards and events declared
        // in the manifest constructed for all hardforks enabled. Ref. https://github.com/neo-project/neo/pull/3195.
        // GasToken is now managed by Governance contract through TokenManagement and is not a native contract anymore.
        // So we only check NeoToken here.
        var neoState = Call_GetContract(snapshot, NativeContract.NEO.Hash, persistingBlock);
        Assert.IsTrue(neoState.Manifest.SupportedStandards.Contains("NEP-17"), "NEO should support NEP-17 standard");
        Assert.AreEqual(1, neoState.Manifest.Abi.Events.Where(e => e.Name == "Transfer").Count(), "NEO should have Transfer event");
    }

    [TestMethod]
    public void TestNativeContractId()
    {
        // native contract id is implicitly defined in NativeContract.cs(the defined order)
        Assert.AreEqual(-1, NativeContract.ContractManagement.Id);
        Assert.AreEqual(-2, NativeContract.StdLib.Id);
        Assert.AreEqual(-3, NativeContract.CryptoLib.Id);
        Assert.AreEqual(-4, NativeContract.Ledger.Id);
        Assert.AreEqual(-7, NativeContract.Policy.Id);
        Assert.AreEqual(-8, NativeContract.RoleManagement.Id);
        Assert.AreEqual(-9, NativeContract.Oracle.Id);
        Assert.AreEqual(-10, NativeContract.Notary.Id);
        Assert.AreEqual(-11, NativeContract.Treasury.Id);
        Assert.AreEqual(-12, NativeContract.TokenManagement.Id);
        Assert.AreEqual(-13, NativeContract.Governance.Id);
        Assert.AreEqual(-14, NativeContract.NEO.Id);
    }

    class TestSpecialParameter
    {
        [ContractMethod]
        public static void TestReadOnlyStoreView(UInt160 address, IReadOnlyStore view) { }

        [ContractMethod]
        public static void TestDataCache(UInt160 address, DataCache cache) { }

        [ContractMethod]
        public static void TestApplicationEngine(ApplicationEngine engine, IReadOnlyStore view) { }

        [ContractMethod]
        public static void TestSnapshot(DataCache cache, ApplicationEngine engine) { }
    }

    [TestMethod]
    public void TestContractMethodWithSpecialParameter()
    {
        // If a contract method has ApplicationEngine, IReadOnlyStoreView or DataCache as a parameter,
        // it should be the first parameter.
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var contract in NativeContract.Contracts)
        {
            foreach (var member in typeof(Contract).GetMembers(flags))
            {
                if (member.GetCustomAttributes<ContractMethodAttribute>().Any())
                    CheckSpecialParameter(member);
            }
        }

        var test = new TestSpecialParameter();
        foreach (var method in typeof(TestSpecialParameter).GetMethods(flags))
        {
            if (method.GetCustomAttributes<ContractMethodAttribute>().Any())
            {
                // should be failed
                Assert.ThrowsExactly<AssertFailedException>(() => CheckSpecialParameter(method));
            }
        }
    }

    private static void CheckSpecialParameter(MemberInfo member)
    {
        var handler = member switch
        {
            MethodInfo m => m,
            PropertyInfo p => p.GetMethod,
            _ => null,
        };
        Assert.IsNotNull(handler, $"handler is null, {member.Name}");

        var parameters = handler.GetParameters();
        foreach (var param in parameters)
        {
            // ApplicationEngine or it's subclass
            // Implementations of IReadOnlyStoreView
            // DataCache or it's subclass
            if (typeof(ApplicationEngine).IsAssignableFrom(param.ParameterType) ||
                typeof(IReadOnlyStore).IsAssignableFrom(param.ParameterType) ||
                typeof(DataCache).IsAssignableFrom(param.ParameterType))
            {
                Assert.AreEqual(0, param.Position);
            }
        }
    }

    [TestMethod]
    public void TestGenesisNativeState()
    {
        var persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 1,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = []
        };
        var snapshot = _snapshotCache.CloneCache();

        // Ensure that all native contracts have proper state generated with an assumption that
        // all hardforks enabled.
        foreach (var ctr in NativeContract.Contracts)
        {
            var state = Call_GetContract(snapshot, ctr.Hash, persistingBlock);
            var actualJson = state.ToJson().ToString();
            if (ctr.Name == "Notary" && !_nativeStates[ctr.Name].Equals(actualJson))
            {
                // Output actual JSON for debugging - write to console
                Console.WriteLine($"Notary actual JSON: {actualJson}");
                Console.WriteLine($"Notary expected JSON: {_nativeStates[ctr.Name]}");
            }
            Assert.AreEqual(_nativeStates[ctr.Name], actualJson, message: $"{ctr.Name} is wrong");
        }
    }

    internal static ContractState Call_GetContract(DataCache snapshot, UInt160 address, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "getContract", address);
        engine.LoadScript(script.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Neo.VM.Types.Array>(result);

        var cs = (ContractState)RuntimeHelpers.GetUninitializedObject(typeof(ContractState));
        ((IInteroperable)cs).FromStackItem(result);

        return cs;
    }
}
