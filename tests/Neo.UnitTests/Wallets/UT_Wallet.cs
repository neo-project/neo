// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Factories;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Sign;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Cryptography;
using Neo.Wallets;
using System.Numerics;
using Helper = Neo.SmartContract.Helper;

namespace Neo.UnitTests.Wallets;

internal class MyWallet : Wallet
{
    public override string Name => "MyWallet";

    public override Version Version => Version.Parse("0.0.1");

    private readonly Dictionary<UInt160, WalletAccount> accounts = new();

    public MyWallet() : base(null!, TestProtocolSettings.Default) { }

    public override bool ChangePassword(string oldPassword, string newPassword)
    {
        throw new NotImplementedException();
    }

    public override bool Contains(UInt160 scriptHash)
    {
        return accounts.ContainsKey(scriptHash);
    }

    public void AddAccount(WalletAccount account)
    {
        accounts.Add(account.ScriptHash, account);
    }

    public override WalletAccount CreateAccount(byte[] privateKey)
    {
        KeyPair key = new(privateKey);
        var contract = new Contract
        {
            Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
            ParameterList = [ContractParameterType.Signature]
        };
        MyWalletAccount account = new(contract.ScriptHash);
        account.SetKey(key);
        account.Contract = contract;
        AddAccount(account);
        return account;
    }

    public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null)
    {
        MyWalletAccount account = new(contract.ScriptHash)
        {
            Contract = contract
        };
        account.SetKey(key!);
        AddAccount(account);
        return account;
    }

    public override WalletAccount CreateAccount(UInt160 scriptHash)
    {
        MyWalletAccount account = new(scriptHash);
        AddAccount(account);
        return account;
    }

    public override void Delete() { }

    public override bool DeleteAccount(UInt160 scriptHash)
    {
        return accounts.Remove(scriptHash);
    }

    public override WalletAccount? GetAccount(UInt160 scriptHash)
    {
        accounts.TryGetValue(scriptHash, out WalletAccount? account);
        return account;
    }

    public override IEnumerable<WalletAccount> GetAccounts()
    {
        return accounts.Values;
    }

    public override bool VerifyPassword(string password)
    {
        return true;
    }

    public override void Save() { }
}

[TestClass]
public class UT_Wallet
{
    private static KeyPair glkey = null!;
    private static string nep2Key = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext ctx)
    {
        glkey = UT_Crypto.GenerateCertainKey(32);
        nep2Key = glkey.Export("pwd", TestProtocolSettings.Default.AddressVersion, 2, 1, 1);
    }

    [TestMethod]
    public void TestContains()
    {
        MyWallet wallet = new();
        try
        {
            wallet.Contains(UInt160.Zero);
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void TestCreateAccount1()
    {
        var wallet = new MyWallet();
        Assert.IsNotNull(wallet.CreateAccount(new byte[32]));
    }

    [TestMethod]
    public void TestCreateAccount2()
    {
        var wallet = new MyWallet();
        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        var account = wallet.CreateAccount(contract, UT_Crypto.GenerateCertainKey(32).PrivateKey);
        Assert.IsNotNull(account);
    }

    [TestMethod]
    public void TestCreateAccount3()
    {
        var wallet = new MyWallet();
        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        Assert.IsNotNull(wallet.CreateAccount(contract, glkey));
    }

    [TestMethod]
    public void TestCreateAccount4()
    {
        var wallet = new MyWallet();
        Assert.IsNotNull(wallet.CreateAccount(UInt160.Zero));
    }

    [TestMethod]
    public void TestGetName()
    {
        var wallet = new MyWallet();
        Assert.AreEqual("MyWallet", wallet.Name);
    }

    [TestMethod]
    public void TestGetVersion()
    {
        var wallet = new MyWallet();
        Assert.AreEqual(Version.Parse("0.0.1"), wallet.Version);
    }

    [TestMethod]
    public void TestGetAccount1()
    {
        var wallet = new MyWallet();
        wallet.CreateAccount(UInt160.Parse("0x7efe7ee0d3e349e085388c351955e5172605de66"));
        var account = wallet.GetAccount(ECCurve.Secp256r1.G)!;
        Assert.AreEqual(UInt160.Parse("0x7efe7ee0d3e349e085388c351955e5172605de66"), account.ScriptHash);
    }

    [TestMethod]
    public void TestGetAccount2()
    {
        var wallet = new MyWallet();

        try
        {
            wallet.GetAccount(UInt160.Zero);
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void TestGetAccounts()
    {
        var wallet = new MyWallet();
        try
        {
            wallet.GetAccounts();
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void TestGetAvailable()
    {
        var wallet = new MyWallet();
        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        var account = wallet.CreateAccount(contract, glkey.PrivateKey);
        account.Lock = false;

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        Assert.AreEqual(new BigDecimal(new BigInteger(1000000000000M), 8), wallet.GetAvailable(snapshotCache, NativeContract.Governance.GasTokenId));

        // Clean up - use the same key format
        key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 0;
    }

    [TestMethod]
    public void TestGetBalance()
    {
        var wallet = new MyWallet();
        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        var account = wallet.CreateAccount(contract, glkey.PrivateKey);
        account.Lock = false;

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        Assert.AreEqual(new BigDecimal(BigInteger.Zero, 0),
            wallet.GetBalance(snapshotCache, UInt160.Zero, [account.ScriptHash]));
        Assert.AreEqual(new BigDecimal(new BigInteger(1000000000000M), 8),
            wallet.GetBalance(snapshotCache, NativeContract.Governance.GasTokenId, [account.ScriptHash]));

        // Clean up - use the same key format
        key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 0;
    }

    [TestMethod]
    public void TestGetPrivateKeyFromNEP2()
    {
        Action action = () => Wallet.GetPrivateKeyFromNEP2("3vQB7B6MrGQZaxCuFg4oh", "TestGetPrivateKeyFromNEP2",
            ProtocolSettings.Default.AddressVersion, 2, 1, 1);
        Assert.ThrowsExactly<FormatException>(action);

        action = () => Wallet.GetPrivateKeyFromNEP2(nep2Key, "Test", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
        Assert.ThrowsExactly<FormatException>(action);

        CollectionAssert.AreEqual("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f".HexToBytes(),
            Wallet.GetPrivateKeyFromNEP2(nep2Key, "pwd", ProtocolSettings.Default.AddressVersion, 2, 1, 1));
    }

    [TestMethod]
    public void TestGetPrivateKeyFromWIF()
    {
        Assert.ThrowsExactly<FormatException>(() => Wallet.GetPrivateKeyFromWIF("3vQB7B6MrGQZaxCuFg4oh"));

        CollectionAssert.AreEqual("c7134d6fd8e73d819e82755c64c93788d8db0961929e025a53363c4cc02a6962".HexToBytes(),
            Wallet.GetPrivateKeyFromWIF("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU"));
    }

    [TestMethod]
    public void TestImport1()
    {
        var wallet = new MyWallet();
        Assert.IsNotNull(wallet.Import("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU"));
    }

    [TestMethod]
    public void TestImport2()
    {
        var wallet = new MyWallet();
        Assert.IsNotNull(wallet.Import(nep2Key, "pwd", 2, 1, 1));
    }

    [TestMethod]
    public void TestMakeTransaction1()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var wallet = new MyWallet();
        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        var account = wallet.CreateAccount(contract, glkey.PrivateKey);
        account.Lock = false;

        Action action = () => wallet.MakeTransaction(snapshotCache, [
            new()
            {
                AssetId = NativeContract.Governance.GasTokenId,
                ScriptHash = account.ScriptHash,
                Value = new BigDecimal(BigInteger.One, 8),
                Data = "Dec 12th"
            }
        ], UInt160.Zero);
        Assert.ThrowsExactly<InvalidOperationException>(action);

        action = () => wallet.MakeTransaction(snapshotCache, [
            new()
            {
                AssetId = NativeContract.Governance.GasTokenId,
                ScriptHash = account.ScriptHash,
                Value = new BigDecimal(BigInteger.One, 8),
                Data = "Dec 12th"
            }
        ], account.ScriptHash);
        Assert.ThrowsExactly<InvalidOperationException>(action);

        action = () => wallet.MakeTransaction(snapshotCache, [
            new()
            {
                 AssetId = UInt160.Zero,
                 ScriptHash = account.ScriptHash,
                 Value = new BigDecimal(BigInteger.One,8),
                 Data = "Dec 12th"
            }
        ], account.ScriptHash);
        Assert.ThrowsExactly<InvalidOperationException>(action);

        // Fake balance - GasToken uses TokenManagement
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        var gasKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry1 = snapshotCache.GetAndChange(gasKey, () => new StorageItem(new AccountState()));
        entry1.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        var neoKey = NativeContract.NEO.CreateStorageKey(20, account.ScriptHash);
        var entry2 = snapshotCache.GetAndChange(neoKey, () => new StorageItem(new NeoToken.NeoAccountState()));
        entry2.GetInteroperable<NeoToken.NeoAccountState>().Balance = 10000 * NativeContract.NEO.Factor;

        var tx = wallet.MakeTransaction(snapshotCache, [
            new()
            {
                 AssetId = NativeContract.Governance.GasTokenId,
                 ScriptHash = account.ScriptHash,
                 Value = new BigDecimal(BigInteger.One,8)
            }
        ]);
        Assert.IsNotNull(tx);

        tx = wallet.MakeTransaction(snapshotCache, [
            new()
            {
                 AssetId = NativeContract.NEO.Hash,
                 ScriptHash = account.ScriptHash,
                 Value = new BigDecimal(BigInteger.One,8),
                 Data = "Dec 12th"
            }
        ]);
        Assert.IsNotNull(tx);

        entry1 = snapshotCache.GetAndChange(gasKey, () => new StorageItem(new AccountState()));
        entry1.GetInteroperable<AccountState>().Balance = 0;
        entry2 = snapshotCache.GetAndChange(neoKey, () => new StorageItem(new NeoToken.NeoAccountState()));
        entry2.GetInteroperable<NeoToken.NeoAccountState>().Balance = 0;
    }

    [TestMethod]
    public void TestMakeTransaction2()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var wallet = new MyWallet();
        Assert.ThrowsExactly<InvalidOperationException>(() => wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), null, null, []));

        var contract = Contract.Create([ContractParameterType.Boolean], [1]);
        var account = wallet.CreateAccount(contract, glkey.PrivateKey);
        account.Lock = false;

        // Fake balance - GasToken uses TokenManagement
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 1000000 * Governance.GasTokenFactor;

        var tx = wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), account.ScriptHash, [
            new()
            {
                Account = account.ScriptHash,
                Scopes = WitnessScope.CalledByEntry
            }
        ], []);

        Assert.IsNotNull(tx);

        tx = wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), null, null, []);
        Assert.IsNotNull(tx);

        entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 0;
    }

    [TestMethod]
    public void TestVerifyPassword()
    {
        var wallet = new MyWallet();
        try
        {
            wallet.VerifyPassword("Test");
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void TestSign()
    {
        var wallet = new MyWallet();
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var network = TestProtocolSettings.Default.Network;
        var block = TestUtils.MakeBlock(snapshotCache, UInt256.Zero, 0);

        Action action = () => wallet.SignBlock(block, glkey.PublicKey, network);
        Assert.ThrowsExactly<SignException>(action); // no account

        wallet.CreateAccount(glkey.PrivateKey);

        var signature = wallet.SignBlock(block, glkey.PublicKey, network);
        Assert.AreEqual(64, signature.Length);

        var signData = block.GetSignData(network);
        var isValid = Crypto.VerifySignature(signData, signature.Span, glkey.PublicKey);
        Assert.IsTrue(isValid);

        var key = new byte[32];
        Array.Fill(key, (byte)0x02);

        var pair = new KeyPair(key);
        var scriptHash = Contract.CreateSignatureRedeemScript(pair.PublicKey).ToScriptHash();
        wallet.CreateAccount(scriptHash);
        Assert.IsNotNull(pair.PublicKey);

        action = () => wallet.SignBlock(block, pair.PublicKey, network);
        Assert.ThrowsExactly<SignException>(action); // no private key

        wallet.GetAccount(scriptHash)!.Lock = true;
        action = () => wallet.SignBlock(block, pair.PublicKey, network);
        Assert.ThrowsExactly<SignException>(action); // locked
    }

    [TestMethod]
    public void TestContainsKeyPair()
    {
        var wallet = new MyWallet();
        var contains = wallet.ContainsSignable(glkey.PublicKey);
        Assert.IsFalse(contains);

        wallet.CreateAccount(glkey.PrivateKey);

        contains = wallet.ContainsSignable(glkey.PublicKey);
        Assert.IsTrue(contains);

        var key = new byte[32];
        Array.Fill(key, (byte)0x01);

        var pair = new KeyPair(key);
        contains = wallet.ContainsSignable(pair.PublicKey);
        Assert.IsFalse(contains);

        wallet.CreateAccount(pair.PrivateKey);
        contains = wallet.ContainsSignable(pair.PublicKey);
        Assert.IsTrue(contains);

        contains = wallet.ContainsSignable(glkey.PublicKey);
        Assert.IsTrue(contains);

        key = new byte[32];
        Array.Fill(key, (byte)0x02);

        pair = new KeyPair(key);
        var scriptHash = Contract.CreateSignatureRedeemScript(pair.PublicKey).ToScriptHash();
        wallet.CreateAccount(scriptHash);

        contains = wallet.ContainsSignable(pair.PublicKey);
        Assert.IsFalse(contains); // no private key

        wallet.GetAccount(scriptHash)!.Lock = true;
        contains = wallet.ContainsSignable(pair.PublicKey);
        Assert.IsFalse(contains); // locked
    }

    [TestMethod]
    public void TestMultiSigAccount()
    {
        var expectedWallet = new MyWallet();
        var expectedPrivateKey1 = RandomNumberFactory.NextBytes(32, cryptography: true);
        var expectedPrivateKey2 = RandomNumberFactory.NextBytes(32, cryptography: true);
        var expectedPrivateKey3 = RandomNumberFactory.NextBytes(32, cryptography: true);

        var expectedWalletAccount1 = expectedWallet.CreateAccount(expectedPrivateKey1);
        var expectedWalletAccount2 = expectedWallet.CreateAccount(expectedPrivateKey2);
        var expectedWalletAccount3 = expectedWallet.CreateAccount(expectedPrivateKey3);

        var expectedAccountKey1 = expectedWalletAccount1.GetKey()!;
        var expectedAccountKey2 = expectedWalletAccount2.GetKey()!;
        var expectedAccountKey3 = expectedWalletAccount3.GetKey()!;

        var actualMultiSigAccount1 = expectedWallet.CreateMultiSigAccount([expectedAccountKey1.PublicKey]);
        var actualMultiSigAccount2 = expectedWallet.CreateMultiSigAccount([expectedAccountKey1.PublicKey, expectedAccountKey2.PublicKey, expectedAccountKey3.PublicKey]);

        Assert.IsNotNull(actualMultiSigAccount1);
        Assert.AreNotEqual(expectedWalletAccount1.ScriptHash, actualMultiSigAccount1.ScriptHash);
        Assert.AreEqual(expectedAccountKey1.PublicKey, actualMultiSigAccount1.GetKey()!.PublicKey);
        Assert.IsTrue(Helper.IsMultiSigContract(actualMultiSigAccount1.Contract!.Script));
        Assert.IsTrue(expectedWallet.GetMultiSigAccounts().Contains(actualMultiSigAccount1));

        var notExpectedAccountKeys = new ECPoint[1025];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => expectedWallet.CreateMultiSigAccount());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => expectedWallet.CreateMultiSigAccount(2, [expectedAccountKey1.PublicKey]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => expectedWallet.CreateMultiSigAccount(0, [expectedAccountKey1.PublicKey]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => expectedWallet.CreateMultiSigAccount(1025, notExpectedAccountKeys));

        Assert.IsNotNull(actualMultiSigAccount2);
        Assert.AreNotEqual(expectedWalletAccount2.ScriptHash, actualMultiSigAccount2.ScriptHash);
        Assert.Contains(actualMultiSigAccount2.GetKey()!.PublicKey, [expectedAccountKey1.PublicKey, expectedAccountKey2.PublicKey, expectedAccountKey3.PublicKey]);
        Assert.IsTrue(Helper.IsMultiSigContract(actualMultiSigAccount2.Contract!.Script));
        Assert.IsTrue(expectedWallet.GetMultiSigAccounts().Contains(actualMultiSigAccount2));
    }
}
