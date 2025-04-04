// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Neo.Plugins.RpcServer.Tests
{
    partial class UT_RpcServer
    {
        [TestMethod]
        public void TestOpenWallet()
        {
            const string Path = "wallet-TestOpenWallet.json";
            const string Password = "123456";
            File.WriteAllText(Path, "{\"name\":null,\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[{\"address\":\"NVizn8DiExdmnpTQfjiVY3dox8uXg3Vrxv\",\"label\":null,\"isDefault\":false,\"lock\":false,\"key\":\"6PYPMrsCJ3D4AXJCFWYT2WMSBGF7dLoaNipW14t4UFAkZw3Z9vQRQV1bEU\",\"contract\":{\"script\":\"DCEDaR\\u002BFVb8lOdiMZ/wCHLiI\\u002Bzuf17YuGFReFyHQhB80yMpBVuezJw==\",\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false},\"extra\":null}],\"extra\":null}");
            var paramsArray = new JArray(Path, Password);
            var res = _rpcServer.OpenWallet(paramsArray);
            Assert.IsTrue(res.AsBoolean());
            Assert.IsNotNull(_rpcServer.wallet);
            Assert.AreEqual(_rpcServer.wallet.GetAccounts().FirstOrDefault()!.Address, "NVizn8DiExdmnpTQfjiVY3dox8uXg3Vrxv");
            _rpcServer.CloseWallet([]);
            File.Delete(Path);
            Assert.IsNull(_rpcServer.wallet);
        }

        [TestMethod]
        public void TestOpenInvalidWallet()
        {
            const string Path = "wallet-TestOpenInvalidWallet.json";
            const string Password = "password";
            File.Delete(Path);
            var paramsArray = new JArray(Path, Password);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.OpenWallet(paramsArray), "Should throw RpcException for unsupported wallet");
            Assert.AreEqual(RpcError.WalletNotFound.Code, exception.HResult);

            File.WriteAllText(Path, "{}");
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.OpenWallet(paramsArray), "Should throw RpcException for unsupported wallet");
            File.Delete(Path);
            Assert.AreEqual(RpcError.WalletNotSupported.Code, exception.HResult);
            var result = _rpcServer.CloseWallet(new JArray());
            Assert.IsTrue(result.AsBoolean());
            Assert.IsNull(_rpcServer.wallet);

            File.WriteAllText(Path, "{\"name\":null,\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[{\"address\":\"NVizn8DiExdmnpTQfjiVY3dox8uXg3Vrxv\",\"label\":null,\"isDefault\":false,\"lock\":false,\"key\":\"6PYPMrsCJ3D4AXJCFWYT2WMSBGF7dLoaNipW14t4UFAkZw3Z9vQRQV1bEU\",\"contract\":{\"script\":\"DCEDaR\\u002BFVb8lOdiMZ/wCHLiI\\u002Bzuf17YuGFReFyHQhB80yMpBVuezJw==\",\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false},\"extra\":null}],\"extra\":null}");
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.OpenWallet(paramsArray), "Should throw RpcException for unsupported wallet");
            Assert.AreEqual(RpcError.WalletNotSupported.Code, exception.HResult);
            Assert.AreEqual(exception.Message, "Wallet not supported - Invalid password.");
            File.Delete(Path);
        }

        [TestMethod]
        public void TestDumpPrivKey()
        {
            TestUtilOpenWallet();
            var account = _rpcServer.wallet.GetAccounts().FirstOrDefault();
            Assert.IsNotNull(account);
            var privKey = account.GetKey().Export();
            var address = account.Address;
            var result = _rpcServer.DumpPrivKey(new JArray(address));
            Assert.AreEqual(privKey, result.AsString());
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestDumpPrivKey_AddressNotInWallet()
        {
            TestUtilOpenWallet();
            // Generate a valid address not in the wallet
            var key = new KeyPair(RandomNumberGenerator.GetBytes(32));
            // Correct way to get ScriptHash from PublicKey
            var scriptHashNotInWallet = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            var addressNotInWallet = scriptHashNotInWallet.ToAddress(ProtocolSettings.Default.AddressVersion);

            var ex = Assert.ThrowsExactly<NullReferenceException>(() => _rpcServer.DumpPrivKey(new JArray(addressNotInWallet)));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestDumpPrivKey_InvalidAddressFormat()
        {
            TestUtilOpenWallet();
            var invalidAddress = "NotAValidAddress";
            var ex = Assert.ThrowsExactly<FormatException>(() => _rpcServer.DumpPrivKey(new JArray(invalidAddress)));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestGetNewAddress()
        {
            TestUtilOpenWallet();
            var result = _rpcServer.GetNewAddress([]);
            Assert.IsInstanceOfType(result, typeof(JString));
            Assert.IsTrue(_rpcServer.wallet.GetAccounts().Any(a => a.Address == result.AsString()));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestGetWalletBalance()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.NEO.Hash;
            var paramsArray = new JArray(assetId.ToString());
            var result = _rpcServer.GetWalletBalance(paramsArray);
            Assert.IsInstanceOfType(result, typeof(JObject));
            var json = (JObject)result;
            Assert.IsTrue(json.ContainsProperty("balance"));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestGetWalletBalanceInvalidAsset()
        {
            TestUtilOpenWallet();
            var assetId = UInt160.Zero;
            var paramsArray = new JArray(assetId.ToString());
            var result = _rpcServer.GetWalletBalance(paramsArray);
            Assert.IsInstanceOfType(result, typeof(JObject));
            var json = (JObject)result;
            Assert.IsTrue(json.ContainsProperty("balance"));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestGetWalletBalance_InvalidAssetIdFormat()
        {
            TestUtilOpenWallet();
            var invalidAssetId = "NotAValidAssetID";
            var paramsArray = new JArray(invalidAssetId);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.GetWalletBalance(paramsArray));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            StringAssert.Contains(ex.Message, "Invalid asset id");
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestGetWalletUnclaimedGas()
        {
            TestUtilOpenWallet();
            var result = _rpcServer.GetWalletUnclaimedGas([]);
            Assert.IsInstanceOfType(result, typeof(JString));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestImportPrivKey()
        {
            TestUtilOpenWallet();
            var privKey = _walletAccount.GetKey().Export();
            var paramsArray = new JArray(privKey);
            var result = _rpcServer.ImportPrivKey(paramsArray);
            Assert.IsInstanceOfType(result, typeof(JObject));
            var json = (JObject)result;
            Assert.IsTrue(json.ContainsProperty("address"));
            Assert.IsTrue(json.ContainsProperty("haskey"));
            Assert.IsTrue(json.ContainsProperty("label"));
            Assert.IsTrue(json.ContainsProperty("watchonly"));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestImportPrivKeyNoWallet()
        {
            var privKey = _walletAccount.GetKey().Export();
            var paramsArray = new JArray(privKey);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.ImportPrivKey(paramsArray));
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestImportPrivKey_InvalidWIF()
        {
            TestUtilOpenWallet();
            var invalidWif = "ThisIsAnInvalidWIFString";
            var paramsArray = new JArray(invalidWif);

            // Expect FormatException during WIF decoding
            var ex = Assert.ThrowsExactly<FormatException>(() => _rpcServer.ImportPrivKey(paramsArray));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestImportPrivKey_KeyAlreadyExists()
        {
            TestUtilOpenWallet();
            // Get a key already in the default test wallet
            var existingAccount = _rpcServer.wallet.GetAccounts().First(a => a.HasKey);
            var existingWif = existingAccount.GetKey().Export();
            var paramsArray = new JArray(existingWif);

            // Import the existing key
            var result = (JObject)_rpcServer.ImportPrivKey(paramsArray);

            // Verify the returned account details match the existing one
            Assert.AreEqual(existingAccount.Address, result["address"].AsString());
            Assert.AreEqual(existingAccount.HasKey, result["haskey"].AsBoolean());
            Assert.AreEqual(existingAccount.Label, result["label"]?.AsString());
            Assert.AreEqual(existingAccount.WatchOnly, result["watchonly"].AsBoolean());

            // Ensure no duplicate account was created (check count remains same)
            var initialCount = _rpcServer.wallet.GetAccounts().Count();
            Assert.AreEqual(initialCount, _rpcServer.wallet.GetAccounts().Count(), "Account count should not change when importing existing key.");

            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestCalculateNetworkFee()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txBase64 = Convert.ToBase64String(tx.ToArray());
            var paramsArray = new JArray(txBase64);
            var result = _rpcServer.CalculateNetworkFee(paramsArray);
            Assert.IsInstanceOfType(result, typeof(JObject));
            var json = (JObject)result;
            Assert.IsTrue(json.ContainsProperty("networkfee"));
        }

        [TestMethod]
        public void TestCalculateNetworkFeeNoParam()
        {
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CalculateNetworkFee([]));
            Assert.AreEqual(exception.HResult, RpcError.InvalidParams.Code);
        }

        [TestMethod]
        public void TestListAddressNoWallet()
        {
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.ListAddress([]));
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestListAddress()
        {
            TestUtilOpenWallet();
            var result = _rpcServer.ListAddress([]);
            Assert.IsInstanceOfType(result, typeof(JArray));
            var json = (JArray)result;
            Assert.IsTrue(json.Count > 0);
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendFromNoWallet()
        {
            var assetId = NativeContract.GAS.Hash;
            var from = _walletAccount.Address;
            var to = _walletAccount.Address;
            var amount = "1";
            var paramsArray = new JArray(assetId.ToString(), from, to, amount);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.SendFrom(paramsArray), "Should throw RpcException for insufficient funds");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestSendFrom()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.GAS.Hash;
            var from = _walletAccount.Address;
            var to = _walletAccount.Address;
            var amount = "1";
            var paramsArray = new JArray(assetId.ToString(), from, to, amount);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.SendFrom(paramsArray));
            Assert.AreEqual(exception.HResult, RpcError.InvalidRequest.Code);
            TestUtilCloseWallet();

            _rpcServer.wallet = _wallet;
            JObject resp = (JObject)_rpcServer.SendFrom(paramsArray);
            Assert.AreEqual(resp.Count, 12);
            Assert.AreEqual(resp["sender"], ValidatorAddress);
            JArray signers = (JArray)resp["signers"];
            Assert.AreEqual(signers.Count, 1);
            Assert.AreEqual(signers[0]["account"], ValidatorScriptHash.ToString());
            Assert.AreEqual(signers[0]["scopes"], nameof(WitnessScope.CalledByEntry));
            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestSendMany()
        {
            var from = _walletAccount.Address;
            var to = new JArray { new JObject { ["asset"] = NativeContract.GAS.Hash.ToString(), ["value"] = "1", ["address"] = _walletAccount.Address } };
            var paramsArray = new JArray(from, to);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.SendMany(paramsArray), "Should throw RpcException for insufficient funds");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);

            _rpcServer.wallet = _wallet;
            JObject resp = (JObject)_rpcServer.SendMany(paramsArray);
            Assert.AreEqual(resp.Count, 12);
            Assert.AreEqual(resp["sender"], ValidatorAddress);
            JArray signers = (JArray)resp["signers"];
            Assert.AreEqual(signers.Count, 1);
            Assert.AreEqual(signers[0]["account"], ValidatorScriptHash.ToString());
            Assert.AreEqual(signers[0]["scopes"], nameof(WitnessScope.CalledByEntry));
            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestSendToAddress()
        {
            var assetId = NativeContract.GAS.Hash;
            var to = _walletAccount.Address;
            var amount = "1";
            var paramsArray = new JArray(assetId.ToString(), to, amount);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.SendToAddress(paramsArray), "Should throw RpcException for insufficient funds");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);

            _rpcServer.wallet = _wallet;
            JObject resp = (JObject)_rpcServer.SendToAddress(paramsArray);
            Assert.AreEqual(resp.Count, 12);
            Assert.AreEqual(resp["sender"], ValidatorAddress);
            JArray signers = (JArray)resp["signers"];
            Assert.AreEqual(signers.Count, 1);
            Assert.AreEqual(signers[0]["account"], ValidatorScriptHash.ToString());
            Assert.AreEqual(signers[0]["scopes"], nameof(WitnessScope.CalledByEntry));
            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestSendToAddress_InvalidAssetId()
        {
            TestUtilOpenWallet();
            var invalidAssetId = "NotAnAssetId";
            var to = _walletAccount.Address;
            var amount = "1";
            var paramsArray = new JArray(invalidAssetId, to, amount);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.SendToAddress(paramsArray));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            StringAssert.Contains(ex.Message, "Invalid asset hash");
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendToAddress_InvalidToAddress()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.GAS.Hash;
            var invalidToAddress = "NotAnAddress";
            var amount = "1";
            var paramsArray = new JArray(assetId.ToString(), invalidToAddress, amount);

            var ex = Assert.ThrowsExactly<FormatException>(() => _rpcServer.SendToAddress(paramsArray));
            // Expect FormatException from AddressToScriptHash
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendToAddress_NegativeAmount()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.GAS.Hash;
            var to = _walletAccount.Address;
            var amount = "-1";
            var paramsArray = new JArray(assetId.ToString(), to, amount);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.SendToAddress(paramsArray));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendToAddress_ZeroAmount()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.GAS.Hash;
            var to = _walletAccount.Address;
            var amount = "0";
            var paramsArray = new JArray(assetId.ToString(), to, amount);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.SendToAddress(paramsArray));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            // Implementation checks amount.Sign > 0
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendToAddress_InsufficientFunds()
        {
            TestUtilOpenWallet();
            var assetId = NativeContract.GAS.Hash;
            var to = _walletAccount.Address;
            var hugeAmount = "100000000000000000"; // Exceeds likely balance
            var paramsArray = new JArray(assetId.ToString(), to, hugeAmount);

            // With a huge amount, MakeTransaction might throw InvalidOperationException internally
            // before returning null to trigger the InsufficientFunds RpcException.
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => _rpcServer.SendToAddress(paramsArray));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendMany_InvalidFromAddress()
        {
            TestUtilOpenWallet();
            var invalidFrom = "NotAnAddress";
            var to = new JArray { new JObject { ["asset"] = NativeContract.GAS.Hash.ToString(), ["value"] = "1", ["address"] = _walletAccount.Address } };
            var paramsArray = new JArray(invalidFrom, to);

            var ex = Assert.ThrowsExactly<FormatException>(() => _rpcServer.SendMany(paramsArray));
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestSendMany_EmptyOutputs()
        {
            TestUtilOpenWallet();
            var from = _walletAccount.Address;
            var emptyTo = new JArray(); // Empty output array
            var paramsArray = new JArray(from, emptyTo);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.SendMany(paramsArray));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            StringAssert.Contains(ex.Message, "Argument 'to' can't be empty");
            TestUtilCloseWallet();
        }

        [TestMethod]
        public void TestCloseWallet_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var result = _rpcServer.CloseWallet(new JArray());
            Assert.IsTrue(result.AsBoolean());
        }

        [TestMethod]
        public void TestDumpPrivKey_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.DumpPrivKey(new JArray(_walletAccount.Address)), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestGetNewAddress_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetNewAddress(new JArray()), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestGetWalletBalance_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetWalletBalance(new JArray(NativeContract.NEO.Hash.ToString())), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestGetWalletUnclaimedGas_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetWalletUnclaimedGas(new JArray()), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestImportPrivKey_WhenWalletNotOpen()
        {
            _rpcServer.wallet = null;
            var privKey = _walletAccount.GetKey().Export();
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.ImportPrivKey(new JArray(privKey)), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
        }

        [TestMethod]
        public void TestCalculateNetworkFee_InvalidTransactionFormat()
        {
            var invalidTxBase64 = "invalid_base64";
            var paramsArray = new JArray(invalidTxBase64);
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CalculateNetworkFee(paramsArray), "Should throw RpcException for invalid transaction format");
            Assert.AreEqual(exception.HResult, RpcError.InvalidParams.Code);
        }

        [TestMethod]
        public void TestListAddress_WhenWalletNotOpen()
        {
            // Ensure the wallet is not open
            _rpcServer.wallet = null;

            // Attempt to call ListAddress and expect an RpcException
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.ListAddress(new JArray()));

            // Verify the exception has the expected error code
            Assert.AreEqual(RpcError.NoOpenedWallet.Code, exception.HResult);
        }

        [TestMethod]
        [Obsolete]
        public void TestCancelTransaction()
        {
            TestUtilOpenWallet();
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            snapshot.Commit();
            var paramsArray = new JArray(tx.Hash.ToString(), new JArray(_walletAccount.Address));
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CancelTransaction(paramsArray), "Should throw RpcException for non-existing transaction");

            Assert.AreEqual(RpcError.InsufficientFunds.Code, exception.HResult);

            // Test with invalid transaction id
            var invalidParamsArray = new JArray("invalid_txid", new JArray(_walletAccount.Address));
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CancelTransaction(invalidParamsArray), "Should throw RpcException for invalid txid");
            Assert.AreEqual(exception.HResult, RpcError.InvalidParams.Code);

            // Test with no signer
            invalidParamsArray = new JArray(tx.Hash.ToString());
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CancelTransaction(invalidParamsArray), "Should throw RpcException for invalid txid");
            Assert.AreEqual(exception.HResult, RpcError.BadRequest.Code);

            // Test with null wallet
            _rpcServer.wallet = null;
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.CancelTransaction(paramsArray), "Should throw RpcException for no opened wallet");
            Assert.AreEqual(exception.HResult, RpcError.NoOpenedWallet.Code);
            TestUtilCloseWallet();

            // Test valid cancel
            _rpcServer.wallet = _wallet;
            JObject resp = (JObject)_rpcServer.SendFrom(new JArray(NativeContract.GAS.Hash.ToString(), _walletAccount.Address, _walletAccount.Address, "1"));
            string txHash = resp["hash"].AsString();
            resp = (JObject)_rpcServer.CancelTransaction(new JArray(txHash, new JArray(ValidatorAddress), "1"));
            Assert.AreEqual(resp.Count, 12);
            Assert.AreEqual(resp["sender"], ValidatorAddress);
            JArray signers = (JArray)resp["signers"];
            Assert.AreEqual(signers.Count, 1);
            Assert.AreEqual(signers[0]["account"], ValidatorScriptHash.ToString());
            Assert.AreEqual(signers[0]["scopes"], nameof(WitnessScope.None));
            Assert.AreEqual(resp["attributes"][0]["type"], nameof(TransactionAttributeType.Conflicts));
            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestInvokeContractVerify()
        {
            var scriptHash = UInt160.Parse("0x70cde1619e405cdef363ab66a1e8dce430d798d5");
            var paramsArray = new JArray(scriptHash.ToString());
            var exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.InvokeContractVerify(paramsArray), "Should throw RpcException for unknown contract");
            Assert.AreEqual(exception.HResult, RpcError.UnknownContract.Code);
            // Test with invalid script hash
            var invalidParamsArray = new JArray("invalid_script_hash");
            exception = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.InvokeContractVerify(invalidParamsArray), "Should throw RpcException for invalid script hash");
            Assert.AreEqual(exception.HResult, RpcError.InvalidParams.Code);
            string base64NefFile = "TkVGM05lby5Db21waWxlci5DU2hhcnAgMy43LjQrNjAzNGExODIxY2E3MDk0NjBlYzMxMzZjNzBjMmRjYzNiZWEuLi4AAAAAAGNXAAJ5JgQiGEEtUQgwE84MASDbMEGb9mfOQeY/GIRADAEg2zBBm/ZnzkGSXegxStgkCUrKABQoAzpB\u002BCfsjEBXAAERiEoQeNBBm/ZnzkGSXegxStgkCUrKABQoAzpB\u002BCfsjEDo2WhC";
            string manifest = """{"name":"ContractWithVerify","groups":[],"features":{},"supportedstandards":[],"abi":{"methods":[{"name":"_deploy","parameters":[{"name":"data","type":"Any"},{"name":"update","type":"Boolean"}],"returntype":"Void","offset":0,"safe":false},{"name":"verify","parameters":[],"returntype":"Boolean","offset":31,"safe":false},{"name":"verify","parameters":[{"name":"prefix","type":"Integer"}],"returntype":"Boolean","offset":63,"safe":false}],"events":[]},"permissions":[],"trusts":[],"extra":{"nef":{"optimization":"All"}}}""";
            JObject deployResp = (JObject)_rpcServer.InvokeFunction(new JArray([ContractManagement.ContractManagement.Hash.ToString(),
                "deploy",
                new JArray([
                    new JObject() { ["type"] = nameof(ContractParameterType.ByteArray), ["value"] = base64NefFile },
                    new JObject() { ["type"] = nameof(ContractParameterType.String), ["value"] = manifest },
                ]),
                validatorSigner]));
            Assert.AreEqual(deployResp["state"], nameof(VMState.HALT));
            UInt160 deployedScriptHash = new UInt160(Convert.FromBase64String(deployResp["notifications"][0]["state"]["value"][0]["value"].AsString()));
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = new Transaction
            {
                Nonce = 233, // Restore original nonce
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + _neoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = [new Signer() { Account = ValidatorScriptHash, Scopes = WitnessScope.CalledByEntry }],
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Convert.FromBase64String(deployResp["script"].AsString()),
                Witnesses = null,
            };
            ApplicationEngine engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: _neoSystem.Settings, gas: 1200_0000_0000);
            engine.SnapshotCache.Commit();

            // invoke verify without signer; should return false
            JObject resp = (JObject)_rpcServer.InvokeContractVerify([deployedScriptHash.ToString()]);
            Assert.AreEqual(resp["state"], nameof(VMState.HALT));
            Assert.AreEqual(resp["stack"][0]["value"].AsBoolean(), false);
            // invoke verify with signer; should return true
            resp = (JObject)_rpcServer.InvokeContractVerify([deployedScriptHash.ToString(), new JArray([]), validatorSigner]);
            Assert.AreEqual(resp["state"], nameof(VMState.HALT));
            Assert.AreEqual(resp["stack"][0]["value"].AsBoolean(), true);
            // invoke verify with wrong input value; should FAULT
            resp = (JObject)_rpcServer.InvokeContractVerify([deployedScriptHash.ToString(), new JArray([new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "0" }]), validatorSigner]);
            Assert.AreEqual(resp["state"], nameof(VMState.FAULT));
            Assert.AreEqual(resp["exception"], "Object reference not set to an instance of an object.");
            // invoke verify with 1 param and signer; should return true
            resp = (JObject)_rpcServer.InvokeContractVerify([deployedScriptHash.ToString(), new JArray([new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "32" }]), validatorSigner]);
            Assert.AreEqual(resp["state"], nameof(VMState.HALT));
            Assert.AreEqual(resp["stack"][0]["value"].AsBoolean(), true);
            // invoke verify with 2 param (which does not exist); should throw Exception
            Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.InvokeContractVerify([deployedScriptHash.ToString(), new JArray([new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "32" }, new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "32" }]), validatorSigner]),
                $"Invalid contract verification function - The smart contract {deployedScriptHash} haven't got verify method with 2 input parameters.");
        }

        private void TestUtilOpenWallet([CallerMemberName] string callerMemberName = "")
        {
            try
            {
                // Avoid using the same wallet file for different tests when they are run in parallel
                string path = $"wallet_{callerMemberName}.json";
                const string Password = "123456";
                File.WriteAllText(path, "{\"name\":null,\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[{\"address\":\"NVizn8DiExdmnpTQfjiVY3dox8uXg3Vrxv\",\"label\":null,\"isDefault\":false,\"lock\":false,\"key\":\"6PYPMrsCJ3D4AXJCFWYT2WMSBGF7dLoaNipW14t4UFAkZw3Z9vQRQV1bEU\",\"contract\":{\"script\":\"DCEDaR\\u002BFVb8lOdiMZ/wCHLiI\\u002Bzuf17YuGFReFyHQhB80yMpBVuezJw==\",\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false},\"extra\":null}],\"extra\":null}");
                var paramsArray = new JArray(path, Password);
                _rpcServer.OpenWallet(paramsArray);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void TestUtilCloseWallet()
        {
            try
            {
                const string Path = "wallet-TestUtilCloseWallet.json";
                _rpcServer.CloseWallet([]);
                File.Delete(Path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private UInt160 TestUtilAddTestContract()
        {
            var state = TestUtils.GetContract();

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };

            var snapshot = _neoSystem.GetSnapshotCache();
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            snapshot.Commit();
            return state.Hash;
        }
    }
}
