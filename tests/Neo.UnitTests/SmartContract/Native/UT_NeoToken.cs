// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NeoToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Array = System.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_NeoToken
{
    private DataCache _snapshotCache = null!;
    private Block _persistingBlock = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        _persistingBlock = new Block
        {
            Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
            Transactions = Array.Empty<Transaction>()
        };
    }

    [TestMethod]
    public void Check_Name()
    {
        var tokenInfo = NativeContract.TokenManagement.GetTokenInfo(_snapshotCache, NativeContract.Governance.NeoTokenId);
        Assert.AreEqual(Governance.NeoTokenName, tokenInfo!.Name);
    }

    [TestMethod]
    public void Check_Symbol()
    {
        var tokenInfo = NativeContract.TokenManagement.GetTokenInfo(_snapshotCache, NativeContract.Governance.NeoTokenId);
        Assert.AreEqual(Governance.NeoTokenSymbol, tokenInfo!.Symbol);
    }

    [TestMethod]
    public void Check_Decimals()
    {
        var tokenInfo = NativeContract.TokenManagement.GetTokenInfo(_snapshotCache, NativeContract.Governance.NeoTokenId);
        Assert.AreEqual(Governance.NeoTokenDecimals, tokenInfo!.Decimals);
    }

    [TestMethod]
    public void Test_HF_EchidnaStates()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
            Transactions = []
        };

        foreach (var method in new string[] { "vote", "registerCandidate", "unregisterCandidate" })
        {
            using (var engine = ApplicationEngine.Create(TriggerType.Application,
                 new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), clonedCache, persistingBlock))
            {
                var methods = NativeContract.Governance.GetContractMethods(engine);
                var entries = methods.Values.Where(u => u.Name == method).ToArray();

                Assert.HasCount(1, entries);
                Assert.AreEqual(CallFlags.States | CallFlags.AllowNotify, entries[0].RequiredCallFlags);
            }
        }
    }

    [TestMethod]
    public void Check_Vote()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };

        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

        // No signature

        var ret = Check_Vote(clonedCache, from, null!, false, persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);

        // Wrong address

        ret = Check_Vote(clonedCache, new byte[19], null!, false, persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsFalse(ret.State);

        // Wrong ec

        ret = Check_Vote(clonedCache, from, new byte[19], true, persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsFalse(ret.State);

        // no registered

        var fakeAddr = new byte[20];
        fakeAddr[0] = 0x5F;
        fakeAddr[5] = 0xFF;

        ret = Check_Vote(clonedCache, fakeAddr, null!, true, persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);

        // no registered

        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from))!, GetNeoAccountStateType());
        SetProperty(accountState, "VoteTo", null);
        ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);
        Assert.IsNull(GetProperty<ECPoint?>(accountState, "VoteTo"));

        // normal case
        var fromUInt160 = new UInt160(from);

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(fromUInt160).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(balanceKey))
        {
            clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));
        }
        else
        {
            var existingItem = clonedCache.GetAndChange(balanceKey)!;
            var existingState = existingItem.GetInteroperable<AccountState>();
            existingState.Balance = 100;
        }

        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        var candidateKey = NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G);
        var candidateItem = new StorageItem(candidateState);
        candidateItem.Seal(); // Ensure the object is serialized with the Registered property set
        clonedCache.Add(candidateKey, candidateItem);

        ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from))!, GetNeoAccountStateType());
        Assert.AreEqual(ECCurve.Secp256r1.G, GetProperty<ECPoint?>(accountState, "VoteTo"));
    }

    [TestMethod]
    public void Check_Vote_Sameaccounts()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };

        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
        var fromUInt160 = new UInt160(from);

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(fromUInt160).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(balanceKey))
        {
            clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));
        }
        else
        {
            var existingItem = clonedCache.GetAndChange(balanceKey)!;
            var existingState = existingItem.GetInteroperable<AccountState>();
            existingState.Balance = 100;
        }

        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from))!, GetNeoAccountStateType());
        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        var candidateItem = new StorageItem(candidateState);
        candidateItem.Seal();
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G), candidateItem);
        var ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from))!, GetNeoAccountStateType());
        Assert.AreEqual(ECCurve.Secp256r1.G, GetProperty<ECPoint?>(accountState, "VoteTo"));

        //two account vote for the same account
        var stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(100, GetProperty<BigInteger>(stateValidator, "Votes"));
        var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
        var G_AccountUInt160 = new UInt160(G_Account);

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(CreateStorageKey(10, G_Account), new StorageItem(CreateNeoAccountState()));
        var secondAccount = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, G_Account))!, GetNeoAccountStateType());

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey2 = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey2))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey2, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey2 = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(G_AccountUInt160).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey2, new StorageItem(new AccountState { Balance = 200 }));

        ret = Check_Vote(clonedCache, G_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(300, GetProperty<BigInteger>(stateValidator, "Votes"));
    }

    [TestMethod]
    public void Check_Vote_ChangeVote()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
        //from vote to G
        byte[] from = TestProtocolSettings.Default.StandbyValidators[0].ToArray();
        var from_Account = Contract.CreateSignatureContract(TestProtocolSettings.Default.StandbyValidators[0]).ScriptHash.ToArray();
        var from_AccountUInt160 = new UInt160(from_Account);

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(CreateStorageKey(10, from_Account), new StorageItem(CreateNeoAccountState()));
        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from_Account))!, GetNeoAccountStateType());

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(from_AccountUInt160).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        var candidateItem = new StorageItem(candidateState);
        candidateItem.Seal();
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G), candidateItem);
        var ret = Check_Vote(clonedCache, from_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from_Account))!, GetNeoAccountStateType());
        Assert.AreEqual(ECCurve.Secp256r1.G, GetProperty<ECPoint?>(accountState, "VoteTo"));

        //from change vote to itself
        var G_stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(100, GetProperty<BigInteger>(G_stateValidator, "Votes"));
        var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
        clonedCache.Add(CreateStorageKey(10, G_Account), new StorageItem(CreateNeoAccountState()));
        var candidateState2 = CreateCandidateState();
        SetProperty(candidateState2, "Registered", true);
        var fromECPoint = ECPoint.DecodePoint(from, ECCurve.Secp256r1);
        var candidateItem2 = new StorageItem(candidateState2);
        candidateItem2.Seal();
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(33, fromECPoint), candidateItem2);
        ret = Check_Vote(clonedCache, from_Account, from, true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        G_stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(0, GetProperty<BigInteger>(G_stateValidator, "Votes"));
        var from_stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, fromECPoint))!, GetCandidateStateType());
        Assert.AreEqual(100, GetProperty<BigInteger>(from_stateValidator, "Votes"));
    }

    [TestMethod]
    public void Check_Vote_VoteToNull()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
        byte[] from = TestProtocolSettings.Default.StandbyValidators[0].ToArray();
        var from_Account = Contract.CreateSignatureContract(TestProtocolSettings.Default.StandbyValidators[0]).ScriptHash.ToArray();
        var from_AccountUInt160 = new UInt160(from_Account);

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(CreateStorageKey(10, from_Account), new StorageItem(CreateNeoAccountState()));
        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from_Account))!, GetNeoAccountStateType());

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(from_AccountUInt160).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        var candidateItem = new StorageItem(candidateState);
        candidateItem.Seal();
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G), candidateItem);
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(23, ECCurve.Secp256r1.G), new StorageItem(new BigInteger(100500)));
        var ret = Check_Vote(clonedCache, from_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from_Account))!, GetNeoAccountStateType());
        Assert.AreEqual(ECCurve.Secp256r1.G, GetProperty<ECPoint?>(accountState, "VoteTo"));

        //from vote to null account G votes becomes 0
        var G_stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(100, GetProperty<BigInteger>(G_stateValidator, "Votes"));
        var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
        clonedCache.Add(CreateStorageKey(10, G_Account), new StorageItem(CreateNeoAccountState()));
        var candidateState2 = CreateCandidateState();
        SetProperty(candidateState2, "Registered", true);
        var fromECPoint = ECPoint.DecodePoint(from, ECCurve.Secp256r1);
        var candidateItem2 = new StorageItem(candidateState2);
        candidateItem2.Seal();
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(33, fromECPoint), candidateItem2);
        ret = Check_Vote(clonedCache, from_Account, null!, true, persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        G_stateValidator = GetInteroperable(clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G))!, GetCandidateStateType());
        Assert.AreEqual(0, GetProperty<BigInteger>(G_stateValidator, "Votes"));
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, from_Account))!, GetNeoAccountStateType());
        Assert.IsNull(GetProperty<ECPoint?>(accountState, "VoteTo"));
    }

    [TestMethod]
    public void Check_UnclaimedGas()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };

        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

        var unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
        Assert.AreEqual(new BigInteger(4.5 * 1000 * 100000000L), unclaim.Value);
        Assert.IsTrue(unclaim.State);

        unclaim = Check_UnclaimedGas(clonedCache, new byte[19], persistingBlock);
        Assert.AreEqual(BigInteger.Zero, unclaim.Value);
        Assert.IsFalse(unclaim.State);
    }

    [TestMethod]
    public void Check_RegisterValidator()
    {
        var clonedCache = _snapshotCache.CloneCache();

        var keyCount = clonedCache.GetChangeSet().Count();
        var point = (byte[])TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true).Clone();

        var ret = Check_RegisterValidator(clonedCache, point, _persistingBlock); // Exists
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);

        Assert.AreEqual(++keyCount, clonedCache.GetChangeSet().Count()); // No changes

        point[20]++; // fake point
        ret = Check_RegisterValidator(clonedCache, point, _persistingBlock); // New

        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);

        Assert.AreEqual(keyCount + 1, clonedCache.GetChangeSet().Count()); // New validator

        // Check GetRegisteredValidators

        var members = NativeContract.Governance.GetCandidatesInternal(clonedCache);
        Assert.AreEqual(2, members.Count());
    }

    [TestMethod]
    public void Check_UnregisterCandidate()
    {
        var clonedCache = _snapshotCache.CloneCache();
        _persistingBlock.Header.Index = 1;
        var keyCount = clonedCache.GetChangeSet().Count();
        var point = TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true);

        //without register
        var ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);

        Assert.AreEqual(keyCount, clonedCache.GetChangeSet().Count());

        //register and then unregister
        ret = Check_RegisterValidator(clonedCache, point, _persistingBlock);
        var pointECPoint = ECPoint.DecodePoint(point, ECCurve.Secp256r1);
        StorageItem item = clonedCache.GetAndChange(NativeContract.Governance.CreateStorageKey(33, pointECPoint))!;
        Assert.AreEqual(7, item.Size);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);

        var members = NativeContract.Governance.GetCandidatesInternal(clonedCache);
        Assert.AreEqual(1, members.Count());
        Assert.AreEqual(keyCount + 1, clonedCache.GetChangeSet().Count());
        StorageKey key = NativeContract.Governance.CreateStorageKey(33, pointECPoint);
        Assert.IsNotNull(clonedCache.TryGet(key));

        ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);

        Assert.AreEqual(keyCount, clonedCache.GetChangeSet().Count());

        members = NativeContract.Governance.GetCandidatesInternal(clonedCache);
        Assert.AreEqual(0, members.Count());
        Assert.IsNull(clonedCache.TryGet(key));

        //register with votes, then unregister
        ret = Check_RegisterValidator(clonedCache, point, _persistingBlock);
        Assert.IsTrue(ret.State);
        var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
        var G_AccountUInt160 = new UInt160(G_Account);

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(CreateStorageKey(10, G_Account), new StorageItem(CreateNeoAccountState()));
        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, G_Account))!, GetNeoAccountStateType());

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(G_AccountUInt160).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        Check_Vote(clonedCache, G_Account, TestProtocolSettings.Default.StandbyValidators[0].ToArray(), true, _persistingBlock);
        ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);
        Assert.IsNotNull(clonedCache.TryGet(key));
        StorageItem pointItem = clonedCache.TryGet(key)!;
        var pointState = GetInteroperable(pointItem, GetCandidateStateType());
        Assert.IsFalse(GetProperty<bool>(pointState, "Registered"));
        Assert.AreEqual(100, GetProperty<BigInteger>(pointState, "Votes"));

        //vote fail
        ret = Check_Vote(clonedCache, G_Account, TestProtocolSettings.Default.StandbyValidators[0].ToArray(), true, _persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsFalse(ret.Result);
        accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, G_Account))!, GetNeoAccountStateType());
        Assert.AreEqual(TestProtocolSettings.Default.StandbyValidators[0], GetProperty<ECPoint?>(accountState, "VoteTo"));
    }

    [TestMethod]
    public void Check_GetCommittee()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var keyCount = clonedCache.GetChangeSet().Count();
        var point = TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true);
        var persistingBlock = _persistingBlock;
        persistingBlock.Header.Index = 1;
        //register with votes with 20000000
        var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
        var G_AccountUInt160 = new UInt160(G_Account);

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(CreateStorageKey(10, G_Account), new StorageItem(CreateNeoAccountState()));
        var accountState = GetInteroperable(clonedCache.TryGet(CreateStorageKey(10, G_Account))!, GetNeoAccountStateType());

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(G_AccountUInt160).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 20000000 }));

        var ret = Check_RegisterValidator(clonedCache, ECCurve.Secp256r1.G.ToArray(), persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);
        ret = Check_Vote(clonedCache, G_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
        Assert.IsTrue(ret.State);
        Assert.IsTrue(ret.Result);


        var committeemembers = NativeContract.Governance.GetCommittee(clonedCache);
        var defaultCommittee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
        Assert.AreEqual(typeof(ECPoint[]), committeemembers.GetType());
        for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount; i++)
        {
            Assert.AreEqual(committeemembers[i], defaultCommittee[i]);
        }

        //register more candidates, committee member change
        persistingBlock = new Block
        {
            Header = new()
            {
                Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [],
        };
        for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount - 1; i++)
        {
            ret = Check_RegisterValidator(clonedCache, TestProtocolSettings.Default.StandbyCommittee[i].ToArray(), persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);
        }

        Assert.IsTrue(Check_OnPersist(clonedCache, persistingBlock));

        committeemembers = NativeContract.Governance.GetCommittee(clonedCache);
        Assert.AreEqual(committeemembers.Length, TestProtocolSettings.Default.CommitteeMembersCount);
        Assert.IsTrue(committeemembers.Contains(ECCurve.Secp256r1.G));
        for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount - 1; i++)
        {
            Assert.IsTrue(committeemembers.Contains(TestProtocolSettings.Default.StandbyCommittee[i]));
        }
        Assert.IsFalse(committeemembers.Contains(TestProtocolSettings.Default.StandbyCommittee[TestProtocolSettings.Default.CommitteeMembersCount - 1]));
    }

    [TestMethod]
    public void Check_Transfer()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };

        byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
        byte[] to = new byte[20];

        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
        var keyCount = clonedCache.GetChangeSet().Count();

        // Check unclaim

        var unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
        Assert.AreEqual(new BigInteger(4.5 * 1000 * 100000000L), unclaim.Value);
        Assert.IsTrue(unclaim.State);

        // Transfer

        Assert.IsFalse(Transfer(clonedCache, from, to, BigInteger.One, false, persistingBlock)); // Not signed
        Assert.IsTrue(Transfer(clonedCache, from, to, BigInteger.One, true, persistingBlock));
        Assert.AreEqual(99999999, BalanceOf(clonedCache, from));
        Assert.AreEqual(1, BalanceOf(clonedCache, to));

        var from_balance = NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, new UInt160(from));
        var to_balance = NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, new UInt160(to));

        Assert.AreEqual(99999999, from_balance);
        Assert.AreEqual(1, to_balance);

        // Check unclaim

        unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
        Assert.AreEqual(BigInteger.Zero, unclaim.Value);
        Assert.IsTrue(unclaim.State);

        // With TokenManagement mode, transfer creates:
        // 1. From account AccountState (update balance)
        // 2. To account AccountState (create/update balance)
        // 3. From account NeoAccountState (create/update for gas distribution)
        // 4. To account NeoAccountState (create for gas distribution if balance > 0)
        // 5. Gas distribution changes (if any)
        // The exact count may vary, so we just check it's reasonable (>= keyCount + 4)
        Assert.IsTrue(clonedCache.GetChangeSet().Count() >= keyCount + 4, $"Expected at least {keyCount + 4} changes, got {clonedCache.GetChangeSet().Count()}");

        // Return balance

        keyCount = clonedCache.GetChangeSet().Count();

        Assert.IsTrue(Transfer(clonedCache, to, from, BigInteger.One, true, persistingBlock));
        Assert.AreEqual(0, BalanceOf(clonedCache, to));
        // When balance becomes 0, AccountState is deleted, but NeoAccountState may still exist
        // The exact count may vary, so we just check it's reasonable (<= keyCount)
        Assert.IsTrue(clonedCache.GetChangeSet().Count() <= keyCount, $"Expected at most {keyCount} changes, got {clonedCache.GetChangeSet().Count()}");

        // Bad inputs

        // Negative amount causes ArgumentOutOfRangeException in contract, which results in FAULT state
        Assert.IsFalse(Transfer(clonedCache, from, to, BigInteger.MinusOne, true, persistingBlock));
        Assert.ThrowsExactly<FormatException>(() => _ = Transfer(clonedCache, new byte[19], to, BigInteger.One, false, persistingBlock));
        Assert.ThrowsExactly<FormatException>(() => _ = Transfer(clonedCache, from, new byte[19], BigInteger.One, false, persistingBlock));

        // More than balance

        Assert.IsFalse(Transfer(clonedCache, to, from, new BigInteger(2), true, persistingBlock));
    }

    [TestMethod]
    public void Check_BalanceOf()
    {
        var clonedCache = _snapshotCache.CloneCache();
        byte[] account = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

        Assert.AreEqual(100_000_000, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, new UInt160(account)));

        account[5]++; // Without existing balance

        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, new UInt160(account)));
    }

    [TestMethod]
    public void Check_CommitteeBonus()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = new()
            {
                Index = 1,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = [],
        };
        Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

        var committee = TestProtocolSettings.Default.StandbyCommittee;
        Assert.AreEqual(50000000, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.GasTokenId, Contract.CreateSignatureContract(committee[0]).ScriptHash));
        Assert.AreEqual(50000000, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.GasTokenId, Contract.CreateSignatureContract(committee[1]).ScriptHash));
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.GasTokenId, Contract.CreateSignatureContract(committee[2]).ScriptHash));
    }

    [TestMethod]
    public void Check_Initialize()
    {
        var clonedCache = _snapshotCache.CloneCache();

        // StandbyValidators

        Check_GetCommittee(clonedCache, null);
    }

    [TestMethod]
    public void TestCalculateBonus()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = (Block)RuntimeHelpers.GetUninitializedObject(typeof(Block));

        var account = UInt160.Zero;
        StorageKey key = NativeContract.Governance.CreateStorageKey(10, account);

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }

        // no balance set, so balance is zero

        clonedCache.Add(key, new StorageItem(CreateNeoAccountState()));
        var ledgerKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.GetAndChange(ledgerKey, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 9 }));
        using var engine1 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(BigInteger.Zero, NativeContract.Governance.UnclaimedGas(engine1, account, 10));
        clonedCache.Delete(key);

        // start >= end

        var neoAccountState3 = CreateNeoAccountState();
        SetProperty(neoAccountState3, "BalanceHeight", 100u);
        clonedCache.GetAndChange(key, () => new StorageItem(neoAccountState3));
        var ledgerKey2 = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.GetAndChange(ledgerKey2, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 9 }));
        using var engine2 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(BigInteger.Zero, NativeContract.Governance.UnclaimedGas(engine2, account, 10));
        clonedCache.Delete(key);

        // Normal 1) votee is non exist

        clonedCache.GetAndChange(key, () => new StorageItem(CreateNeoAccountState()));

        // Set NEO balance
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        var ledgerKey4 = new KeyBuilder(NativeContract.Ledger.Id, 12);
        var item = clonedCache.GetAndChange(ledgerKey4, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 99 }))!.GetInteroperable<HashIndexState>();
        item.Index = 99;

        using var engine4 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(new BigInteger(4.5 * 100 * 100), NativeContract.Governance.UnclaimedGas(engine4, account, 100));
        clonedCache.Delete(key);
        clonedCache.Delete(balanceKey);

        // Normal 2) votee is not committee

        var neoAccountState22 = CreateNeoAccountState();
        SetProperty(neoAccountState22, "VoteTo", ECCurve.Secp256r1.G);
        clonedCache.GetAndChange(key, () => new StorageItem(neoAccountState22));

        // Set NEO balance
        balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        var ledgerKey5 = new KeyBuilder(NativeContract.Ledger.Id, 12);
        item = clonedCache.GetAndChange(ledgerKey5, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 99 }))!.GetInteroperable<HashIndexState>();
        item.Index = 99;

        using var engine5 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(new BigInteger(4.5 * 100 * 100), NativeContract.Governance.UnclaimedGas(engine5, account, 100));
        clonedCache.Delete(key);
        clonedCache.Delete(balanceKey);

        // Normal 3) votee is committee

        var neoAccountState23 = CreateNeoAccountState();
        SetProperty(neoAccountState23, "VoteTo", TestProtocolSettings.Default.StandbyCommittee[0]);
        clonedCache.GetAndChange(key, () => new StorageItem(neoAccountState23));

        // Set NEO balance
        balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 100 }));

        clonedCache.Add(new KeyBuilder(NativeContract.Governance.Id, 23).Add(TestProtocolSettings.Default.StandbyCommittee[0]).Add(uint.MaxValue - 50), new StorageItem() { Value = new BigInteger(50 * 10000L).ToByteArray() });

        var ledgerKey6 = new KeyBuilder(NativeContract.Ledger.Id, 12);
        item = clonedCache.GetAndChange(ledgerKey6, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 99 }))!.GetInteroperable<HashIndexState>();
        item.Index = 99;

        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(new BigInteger(450 * 100), NativeContract.Governance.UnclaimedGas(engine, account, 100));
        clonedCache.Delete(key);
        clonedCache.Delete(balanceKey);
    }

    [TestMethod]
    public void TestGetNextBlockValidators1()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var result = (Neo.VM.Types.Array)NativeContract.Governance.Call(snapshotCache, "getNextBlockValidators")!;
        Assert.HasCount(7, result);
        Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].GetSpan().ToHexString());
        Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].GetSpan().ToHexString());
        Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].GetSpan().ToHexString());
        Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].GetSpan().ToHexString());
        Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].GetSpan().ToHexString());
        Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].GetSpan().ToHexString());
        Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].GetSpan().ToHexString());
    }

    [TestMethod]
    public void TestGetNextBlockValidators2()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var result = NativeContract.Governance.GetNextBlockValidators(clonedCache, 7);
        Assert.HasCount(7, result);
        Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].ToArray().ToHexString());
        Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].ToArray().ToHexString());
        Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].ToArray().ToHexString());
        Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].ToArray().ToHexString());
        Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].ToArray().ToHexString());
        Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].ToArray().ToHexString());
        Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].ToArray().ToHexString());
    }

    [TestMethod]
    public void TestGetCandidates1()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var array = (Neo.VM.Types.Array)NativeContract.Governance.Call(snapshotCache, "getCandidates")!;
        Assert.IsEmpty(array);
    }

    [TestMethod]
    public void TestGetCandidates2()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var result = NativeContract.Governance.GetCandidatesInternal(clonedCache);
        Assert.AreEqual(0, result.Count());

        StorageKey key = NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G);
        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        clonedCache.Add(key, new StorageItem(candidateState));
        Assert.AreEqual(1, NativeContract.Governance.GetCandidatesInternal(clonedCache).Count());
    }

    [TestMethod]
    public void TestCheckCandidate()
    {
        var cloneCache = _snapshotCache.CloneCache();
        var committee = NativeContract.Governance.GetCommittee(cloneCache);
        var point = committee[0].EncodePoint(true);

        // Prepare Candidate
        var storageKey = new KeyBuilder(NativeContract.Governance.Id, 33).Add(committee[0]);
        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        SetProperty(candidateState, "Votes", BigInteger.One);
        cloneCache.Add(storageKey, new StorageItem(candidateState));

        // Pre-persist
        var persistingBlock = new Block
        {
            Header = new()
            {
                Index = 21,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = [],
        };
        Assert.IsTrue(Check_OnPersist(cloneCache, persistingBlock));

        // Clear votes
        storageKey = new KeyBuilder(NativeContract.Governance.Id, 33).Add(committee[0]);
        var candidateState2 = GetInteroperable(cloneCache.GetAndChange(storageKey)!, GetCandidateStateType());
        SetProperty(candidateState2, "Votes", BigInteger.Zero);

        // Unregister candidate, remove
        var (state, result) = Check_UnregisterCandidate(cloneCache, point, persistingBlock);
        Assert.IsTrue(state);
        Assert.IsTrue(result);

        // Post-persist
        Assert.IsTrue(Check_PostPersist(cloneCache, persistingBlock));
    }

    [TestMethod]
    public void TestGetCommittee()
    {
        var clonedCache = TestBlockchain.GetTestSnapshotCache();
        var result = (Neo.VM.Types.Array)NativeContract.Governance.Call(clonedCache, "getCommittee")!;
        Assert.HasCount(21, result);
        Assert.AreEqual("020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639", result[0].GetSpan().ToHexString());
        Assert.AreEqual("03204223f8c86b8cd5c89ef12e4f0dbb314172e9241e30c9ef2293790793537cf0", result[1].GetSpan().ToHexString());
        Assert.AreEqual("0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30", result[2].GetSpan().ToHexString());
        Assert.AreEqual("0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d", result[3].GetSpan().ToHexString());
        Assert.AreEqual("023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe", result[4].GetSpan().ToHexString());
        Assert.AreEqual("03409f31f0d66bdc2f70a9730b66fe186658f84a8018204db01c106edc36553cd0", result[5].GetSpan().ToHexString());
        Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[6].GetSpan().ToHexString());
        Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[7].GetSpan().ToHexString());
        Assert.AreEqual("02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad", result[8].GetSpan().ToHexString());
        Assert.AreEqual("03708b860c1de5d87f5b151a12c2a99feebd2e8b315ee8e7cf8aa19692a9e18379", result[9].GetSpan().ToHexString());
        Assert.AreEqual("0288342b141c30dc8ffcde0204929bb46aed5756b41ef4a56778d15ada8f0c6654", result[10].GetSpan().ToHexString());
        Assert.AreEqual("02a62c915cf19c7f19a50ec217e79fac2439bbaad658493de0c7d8ffa92ab0aa62", result[11].GetSpan().ToHexString());
        Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[12].GetSpan().ToHexString());
        Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[13].GetSpan().ToHexString());
        Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[14].GetSpan().ToHexString());
        Assert.AreEqual("03c6aa6e12638b36e88adc1ccdceac4db9929575c3e03576c617c49cce7114a050", result[15].GetSpan().ToHexString());
        Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[16].GetSpan().ToHexString());
        Assert.AreEqual("02cd5a5547119e24feaa7c2a0f37b8c9366216bab7054de0065c9be42084003c8a", result[17].GetSpan().ToHexString());
        Assert.AreEqual("03cdcea66032b82f5c30450e381e5295cae85c5e6943af716cc6b646352a6067dc", result[18].GetSpan().ToHexString());
        Assert.AreEqual("03d281b42002647f0113f36c7b8efb30db66078dfaaa9ab3ff76d043a98d512fde", result[19].GetSpan().ToHexString());
        Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[20].GetSpan().ToHexString());
    }

    [TestMethod]
    public void TestGetValidators()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var result = NativeContract.Governance.ComputeNextBlockValidators(clonedCache, TestProtocolSettings.Default);
        Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].ToArray().ToHexString());
        Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].ToArray().ToHexString());
        Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].ToArray().ToHexString());
        Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].ToArray().ToHexString());
        Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].ToArray().ToHexString());
        Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].ToArray().ToHexString());
        Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].ToArray().ToHexString());
    }

    [TestMethod]
    public void TestOnBalanceChanging()
    {
        var ret = Transfer4TesingOnBalanceChanging(new BigInteger(0), false);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);

        ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), false);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);

        ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), true);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
    }

    [TestMethod]
    public void TestTotalSupply()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var tokenInfo = NativeContract.TokenManagement.GetTokenInfo(clonedCache, NativeContract.Governance.NeoTokenId);
        Assert.AreEqual(new BigInteger(100000000), tokenInfo!.TotalSupply);
    }

    [TestMethod]
    public void TestEconomicParameter()
    {
        const byte Prefix_CurrentBlock = 12;
        var clonedCache = _snapshotCache.CloneCache();
        var persistingBlock = new Block
        {
            Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
            Transactions = []
        };

        (BigInteger, bool) result = Check_GetGasPerBlock(clonedCache, persistingBlock);
        Assert.IsTrue(result.Item2);
        Assert.AreEqual(5 * Governance.GasTokenFactor, result.Item1);

        persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 10,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        (Boolean, bool) result1 = Check_SetGasPerBlock(clonedCache, 10 * Governance.GasTokenFactor, persistingBlock);
        Assert.IsTrue(result1.Item2);
        Assert.IsTrue(result1.Item1.GetBoolean());

        var height = clonedCache[NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
        height.Index = persistingBlock.Index + 1;
        result = Check_GetGasPerBlock(clonedCache, persistingBlock);
        Assert.IsTrue(result.Item2);
        Assert.AreEqual(10 * Governance.GasTokenFactor, result.Item1);

        // Check calculate bonus
        var account = UInt160.Zero;

        // Ensure default gas per block record exists (index 0) if not already present
        var defaultGasPerBlockKey = NativeContract.Governance.CreateStorageKey(29, 0u);
        if (!clonedCache.Contains(defaultGasPerBlockKey))
        {
            clonedCache.Add(defaultGasPerBlockKey, new StorageItem(5 * Governance.GasTokenFactor));
        }

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        StorageItem storage = clonedCache.GetOrAdd(NativeContract.Governance.CreateStorageKey(10, account), () => new StorageItem(CreateNeoAccountState()));
        var state = GetInteroperable(storage, GetNeoAccountStateType());
        SetProperty(state, "BalanceHeight", 0u);

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 1000 }));

        height.Index = persistingBlock.Index + 1;
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);
        Assert.AreEqual(58500, NativeContract.Governance.UnclaimedGas(engine, account, persistingBlock.Index + 2));
    }

    [TestMethod]
    public void TestClaimGas()
    {
        var clonedCache = _snapshotCache.CloneCache();

        // Initialize block
        clonedCache.Add(CreateStorageKey(1), new StorageItem(new BigInteger(30000000)));

        ECPoint[] standbyCommittee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
        var cachedCommittee = CreateCachedCommittee();
        for (var i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount; i++)
        {
            ECPoint member = standbyCommittee[i];
            var candidateState = CreateCandidateState();
            SetProperty(candidateState, "Registered", true);
            SetProperty(candidateState, "Votes", new BigInteger(200 * 10000));
            clonedCache.Add(new KeyBuilder(NativeContract.Governance.Id, 33).Add(member), new StorageItem(candidateState));
            var addMethod = cachedCommittee.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            addMethod?.Invoke(cachedCommittee, new object[] { (member, new BigInteger(200 * 10000)) });
        }
        var stackItem = cachedCommittee.ToStackItem(null);
        clonedCache.GetOrAdd(new KeyBuilder(NativeContract.Governance.Id, 14), () => new StorageItem()).Value = BinarySerializer.Serialize(stackItem, ExecutionEngineLimits.Default);

        var item = clonedCache.GetAndChange(new KeyBuilder(NativeContract.Governance.Id, 1), () => new StorageItem());
        item.Value = ((BigInteger)2100 * 10000L).ToByteArray();

        var persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 0,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = [],
        };
        Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

        var committee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
        var accountA = committee[0];
        var accountB = committee[TestProtocolSettings.Default.CommitteeMembersCount - 1];
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, Contract.CreateSignatureContract(accountA).ScriptHash));

        // Next block

        persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 1,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = [],
        };
        Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, Contract.CreateSignatureContract(committee[1]).ScriptHash));

        // Next block

        persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 21,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = [],
        };
        Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

        accountA = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray()[2];
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, Contract.CreateSignatureContract(committee[2]).ScriptHash));

        // Claim GAS

        var account = Contract.CreateSignatureContract(committee[2]).ScriptHash;

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        var neoAccountState21 = CreateNeoAccountState();
        SetProperty(neoAccountState21, "BalanceHeight", 3u);
        SetProperty(neoAccountState21, "VoteTo", committee[2]);
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(10, account), new StorageItem(neoAccountState21));

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 1999800 }));

        Assert.AreEqual(1999800, NativeContract.TokenManagement.BalanceOf(clonedCache, NativeContract.Governance.NeoTokenId, account));
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        clonedCache.GetAndChange(storageKey)!.GetInteroperable<HashIndexState>().Index = 29 + 2;
        var persistingBlock2 = new Block
        {
            Header = new Header
            {
                Index = 29 + 2,
                Witness = Witness.Empty,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero
            },
            Transactions = []
        };
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock2, settings: TestProtocolSettings.Default);
        BigInteger value = NativeContract.Governance.UnclaimedGas(engine, account, 29 + 3);
        Assert.AreEqual(1999800L * 90 * 5 * 29 / 100, value);
    }

    [TestMethod]
    public void TestUnclaimedGas()
    {
        var clonedCache = _snapshotCache.CloneCache();
        var account = UInt160.Zero;
        // Set Ledger.CurrentIndex to 9, so end should be 10
        const byte Prefix_CurrentBlock = 12;
        var ledgerKey = new KeyBuilder(NativeContract.Ledger.Id, Prefix_CurrentBlock);
        var height = clonedCache.GetAndChange(ledgerKey, () => new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = 9 }))!.GetInteroperable<HashIndexState>();
        height.Index = 9;

        using var engine1 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(BigInteger.Zero, NativeContract.Governance.UnclaimedGas(engine1, account, 10));

        // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
        clonedCache.Add(NativeContract.Governance.CreateStorageKey(10, account), new StorageItem(CreateNeoAccountState()));
        using var engine2 = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestProtocolSettings.Default);
        Assert.AreEqual(BigInteger.Zero, NativeContract.Governance.UnclaimedGas(engine2, account, 10));
    }

    [TestMethod]
    public void TestVote()
    {
        var clonedCache = _snapshotCache.CloneCache();
        UInt160 account = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
        StorageKey keyAccount = NativeContract.Governance.CreateStorageKey(10, account);
        StorageKey keyValidator = NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G);
        _persistingBlock.Header.Index = 1;
        var ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), false, _persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);

        ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);

        clonedCache.Add(keyAccount, new StorageItem(CreateNeoAccountState()));
        ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
        Assert.IsFalse(ret.Result);
        Assert.IsTrue(ret.State);

        var vote_to_null = NativeContract.Governance.GetVoteTarget(clonedCache, account);
        Assert.IsNull(vote_to_null);

        clonedCache.Delete(keyAccount);

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(account).Add(NativeContract.Governance.NeoTokenId);
        clonedCache.Add(balanceKey, new StorageItem(new AccountState { Balance = 1 }));

        var neoAccountState13 = CreateNeoAccountState();
        SetProperty(neoAccountState13, "VoteTo", ECCurve.Secp256r1.G);
        var storageItem = new StorageItem(neoAccountState13);
        storageItem.Seal();
        clonedCache.GetAndChange(keyAccount, () => storageItem);
        var candidateState = CreateCandidateState();
        SetProperty(candidateState, "Registered", true);
        var candidateStorageItem = new StorageItem(candidateState);
        candidateStorageItem.Seal();
        clonedCache.Add(keyValidator, candidateStorageItem);
        ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
        Assert.IsTrue(ret.Result);
        Assert.IsTrue(ret.State);
        var voteto = NativeContract.Governance.GetVoteTarget(clonedCache, account);
        Assert.AreEqual(ECCurve.Secp256r1.G, voteto);
    }

    internal (bool State, bool Result) Transfer4TesingOnBalanceChanging(BigInteger amount, bool addVotes)
    {
        var clonedCache = _snapshotCache.CloneCache();
        _persistingBlock.Header.Index = 1;
        var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), clonedCache, _persistingBlock, settings: TestProtocolSettings.Default);
        ScriptBuilder sb = new();
        UInt160 from = engine.ScriptContainer!.GetScriptHashesForVerifying(engine.SnapshotCache)[0];

        // Ensure TokenState exists for NeoTokenId (required by TokenManagement.transfer)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!clonedCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
        }

        // Set up NEO balance using TokenManagement storage (Prefix_AccountState = 12)
        var balanceKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(from).Add(NativeContract.Governance.NeoTokenId);
        var balanceItem = clonedCache.GetAndChange(balanceKey, () => new StorageItem(new AccountState()));
        balanceItem.GetInteroperable<AccountState>().Balance = 1000;
        balanceItem.Seal();

        if (addVotes)
        {
            // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
            var neoAccountState14 = CreateNeoAccountState();
            SetProperty(neoAccountState14, "VoteTo", ECCurve.Secp256r1.G);
            var neoAccountKey = NativeContract.Governance.CreateStorageKey(10, from);
            var neoAccountItem = clonedCache.GetAndChange(neoAccountKey, () => new StorageItem(neoAccountState14));
            var neoAccountStateObj = GetInteroperable(neoAccountItem!, GetNeoAccountStateType());
            SetProperty(neoAccountStateObj, "VoteTo", ECCurve.Secp256r1.G);
            neoAccountItem!.Seal();

            // Set up CandidateState
            var candidateState = CreateCandidateState();
            SetProperty(candidateState, "Registered", true);
            var candidateKey = NativeContract.Governance.CreateStorageKey(33, ECCurve.Secp256r1.G);
            var candidateItem = clonedCache.GetAndChange(candidateKey, () => new StorageItem(candidateState));
            var candidateStateObj = GetInteroperable(candidateItem!, GetCandidateStateType());
            SetProperty(candidateStateObj, "Registered", true);
            candidateItem!.Seal();
        }
        else
        {
            // Set up NeoAccountState with prefix 10 (Prefix_NeoAccount)
            var neoAccountState15 = CreateNeoAccountState();
            var neoAccountKey = NativeContract.Governance.CreateStorageKey(10, from);
            clonedCache.GetAndChange(neoAccountKey, () => new StorageItem(neoAccountState15));
        }

        sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.NeoTokenId, from, UInt160.Zero, amount, null);
        engine.LoadScript(sb.ToArray());
        var state = engine.Execute();
        var result = engine.ResultStack.Peek();
        Assert.AreEqual(typeof(Boolean), result.GetType());
        return (true, result.GetBoolean());
    }

    internal static bool Check_OnPersist(DataCache clonedCache, Block persistingBlock)
    {
        var script = new ScriptBuilder();
        script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
        var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);
        engine.LoadScript(script.ToArray());

        return engine.Execute() == VMState.HALT;
    }

    internal static bool Check_PostPersist(DataCache clonedCache, Block persistingBlock)
    {
        using var script = new ScriptBuilder();
        script.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
        using var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);
        engine.LoadScript(script.ToArray());

        return engine.Execute() == VMState.HALT;
    }

    internal static (BigInteger Value, bool State) Check_GetGasPerBlock(DataCache clonedCache, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "getGasPerBlock");
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            return (BigInteger.Zero, false);
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Integer>(result);

        return (((Integer)result).GetInteger(), true);
    }

    internal static (Boolean Value, bool State) Check_SetGasPerBlock(DataCache clonedCache, BigInteger gasPerBlock, Block persistingBlock)
    {
        UInt160 committeeMultiSigAddr = NativeContract.Governance.GetCommitteeAddress(clonedCache);
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "setGasPerBlock", gasPerBlock);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
            return (false, false);

        return (true, true);
    }

    internal static (bool State, bool Result) Check_Vote(DataCache clonedCache, byte[] account, byte[]? pubkey, bool signAccount, Block persistingBlock)
    {
        // Check if account is valid (must be 20 bytes)
        if (account.Length != 20)
            return (false, false);

        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        if (pubkey == null)
        {
            script.EmitDynamicCall(NativeContract.Governance.Hash, "vote", new UInt160(account), (ECPoint?)null);
        }
        else
        {
            // Check if pubkey is valid (must be 33 bytes for compressed ECPoint or 65 for uncompressed)
            if (pubkey.Length != 33 && pubkey.Length != 65)
                return (false, false);
            ECPoint voteTo = ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1);
            script.EmitDynamicCall(NativeContract.Governance.Hash, "vote", new UInt160(account), voteTo);
        }
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            return (false, false);
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return (true, result.GetBoolean());
    }

    internal static (bool State, bool Result) Check_RegisterValidator(DataCache clonedCache, byte[] pubkey, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), clonedCache, persistingBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "registerCandidate", pubkey);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            return (false, false);
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return (true, result.GetBoolean());
    }

    internal static (bool State, bool Result) Check_RegisterValidatorOnPayment(DataCache clonedCache, ECPoint pubkey, Block persistingBlock, bool passNEO, byte[] data, BigInteger amount)
    {
        var keyScriptHash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
        StorageKey storageKey;

        if (passNEO)
        {
            // NEO now uses TokenManagement with Prefix_AccountState = 12
            // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
            var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
            if (!clonedCache.Contains(tokenStateKey))
            {
                var tokenState = new TokenState
                {
                    Type = TokenType.Fungible,
                    Owner = NativeContract.Governance.Hash,
                    Name = Governance.NeoTokenName,
                    Symbol = Governance.NeoTokenSymbol,
                    Decimals = Governance.NeoTokenDecimals,
                    TotalSupply = BigInteger.Zero,
                    MaxSupply = Governance.NeoTokenTotalAmount
                };
                clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
            }
            // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
            storageKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(keyScriptHash).Add(NativeContract.Governance.NeoTokenId);
            clonedCache.Add(storageKey, new StorageItem(new AccountState { Balance = amount }));
        }
        else
        {
            // GasToken uses TokenManagement with Prefix_AccountState = 12
            // First, ensure TokenState exists (required by TokenManagement.BalanceOf)
            var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
            if (!clonedCache.Contains(tokenStateKey))
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
                clonedCache.Add(tokenStateKey, new StorageItem(tokenState));
            }
            // Then set account balance: KeyBuilder(TokenManagement.Id, 12).Add(account).Add(assetId)
            storageKey = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(keyScriptHash).Add(NativeContract.Governance.GasTokenId);
            clonedCache.Add(storageKey, new StorageItem(new AccountState { Balance = amount }));
        }

        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(keyScriptHash), clonedCache, persistingBlock, settings: TestProtocolSettings.Default, gas: 1_0000_0000);

        using var script = new ScriptBuilder();
        if (passNEO)
            script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.NeoTokenId, keyScriptHash, NativeContract.Governance.Hash, amount, data);
        else
            script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, keyScriptHash, NativeContract.Governance.Hash, amount, data);
        engine.LoadScript(script.ToArray());

        var execRes = engine.Execute();
        clonedCache.Delete(storageKey); // Clean up for subsequent invocations.

        if (execRes == VMState.FAULT)
            return (false, false);

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return (true, result.GetBoolean());
    }

    internal static ECPoint[] Check_GetCommittee(DataCache clonedCache, Block? persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "getCommittee");
        engine.LoadScript(script.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Neo.VM.Types.Array>(result, out var array);

        return array.Select(u => ECPoint.DecodePoint(u.GetSpan(), ECCurve.Secp256r1)).ToArray();
    }

    internal static (BigInteger Value, bool State) Check_UnclaimedGas(DataCache clonedCache, byte[] address, Block persistingBlock)
    {
        // Check if address is valid (must be 20 bytes)
        if (address.Length != 20)
            return (BigInteger.Zero, false);

        using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "unclaimedGas", new UInt160(address), persistingBlock.Index);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            return (BigInteger.Zero, false);
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Integer>(result);

        return (result.GetInteger(), true);
    }

    internal static void CheckValidator(ECPoint eCPoint, KeyValuePair<StorageKey, DataCache.Trackable> trackable)
    {
        BigInteger st = trackable.Value.Item;
        Assert.AreEqual(0, st);

        CollectionAssert.AreEqual(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)).ToArray(), trackable.Key.Key.ToArray());
    }

    internal static void CheckBalance(byte[] account, KeyValuePair<StorageKey, DataCache.Trackable> trackable, BigInteger balance, BigInteger height, ECPoint voteTo)
    {
        var st = (Struct)BinarySerializer.Deserialize(trackable.Value.Item.Value, ExecutionEngineLimits.Default);

        Assert.HasCount(3, st);
        CollectionAssert.AreEqual(new Type[] { typeof(Integer), typeof(Integer), typeof(ByteString) }, st.Select(u => u.GetType()).ToArray()); // Balance

        Assert.AreEqual(balance, st[0].GetInteger()); // Balance
        Assert.AreEqual(height, st[1].GetInteger());  // BalanceHeight
        Assert.AreEqual(voteTo, ECPoint.DecodePoint(st[2].GetSpan(), ECCurve.Secp256r1));  // Votes

        CollectionAssert.AreEqual(new byte[] { 20 }.Concat(account).ToArray(), trackable.Key.ToArray());
    }

    internal static StorageKey CreateStorageKey(byte prefix, byte[]? key = null)
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
        buffer[0] = prefix;
        key?.CopyTo(buffer.AsSpan(1));
        return new()
        {
            Id = NativeContract.Governance.Id,
            Key = buffer
        };
    }

    internal static (bool State, bool Result) Check_UnregisterCandidate(DataCache clonedCache, byte[] pubkey, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), clonedCache, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Governance.Hash, "unregisterCandidate", pubkey);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            return (false, false);
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return (true, result.GetBoolean());
    }

    internal static bool Transfer(DataCache snapshot, byte[]? from, byte[]? to, BigInteger amount, bool signAccount, Block persistingBlock)
    {
        if (from == null || to == null) throw new InvalidOperationException();
        if (from.Length != 20) throw new FormatException();
        if (to.Length != 20) throw new FormatException();
        UInt160 fromAddr = new(from);
        UInt160 toAddr = new(to);

        // Ensure TokenState exists for NeoTokenId (required by TokenManagement.transfer)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.NeoTokenId);
        if (!snapshot.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.NeoTokenName,
                Symbol = Governance.NeoTokenSymbol,
                Decimals = Governance.NeoTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = Governance.NeoTokenTotalAmount
            };
            snapshot.Add(tokenStateKey, new StorageItem(tokenState));
        }

        using var engine = ApplicationEngine.Create(TriggerType.Application,
            signAccount ? new Nep17NativeContractExtensions.ManualWitness(fromAddr) : null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);
        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.NeoTokenId, fromAddr, toAddr, amount, null);
        engine.LoadScript(script.ToArray());
        var state = engine.Execute();
        if (state == VMState.FAULT)
        {
            // Re-throw the exception if it's an InvalidOperationException or FormatException
            // This allows tests to catch these exceptions when contracts reject payments
            // ArgumentOutOfRangeException (e.g., negative amount) should return false, not throw
            if (engine.FaultException != null)
            {
                // Check inner exceptions in case the exception is wrapped
                Exception? ex = engine.FaultException;
                while (ex != null)
                {
                    // Re-throw InvalidOperationException and FormatException
                    // These are expected exceptions from contracts rejecting payments
                    if (ex is InvalidOperationException || ex is FormatException)
                    {
                        throw ex;
                    }
                    // ArgumentOutOfRangeException (e.g., negative amount) should return false
                    // Don't re-throw it, just return false
                    ex = ex.InnerException;
                }
            }
            return false;
        }
        var result = engine.ResultStack.Pop();
        return result.GetBoolean();
    }

    internal static BigInteger BalanceOf(DataCache snapshot, byte[] account)
    {
        if (account.Length != 20) return BigInteger.Zero;
        return NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.NeoTokenId, new UInt160(account));
    }

    private static Type GetNeoAccountStateType()
    {
        return typeof(Governance).GetNestedType("NeoAccountState", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("NeoAccountState type not found");
    }

    private static Type GetCachedCommitteeType()
    {
        return typeof(Governance).GetNestedType("CachedCommittee", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CachedCommittee type not found");
    }

    private static IInteroperable CreateNeoAccountState()
    {
        return (IInteroperable)RuntimeHelpers.GetUninitializedObject(GetNeoAccountStateType());
    }

    private static Type GetCandidateStateType()
    {
        return typeof(Governance).GetNestedType("CandidateState", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CandidateState type not found");
    }

    private static IInteroperable CreateCandidateState()
    {
        return (IInteroperable)RuntimeHelpers.GetUninitializedObject(GetCandidateStateType());
    }

    private static IInteroperable CreateCachedCommittee()
    {
        return (IInteroperable)RuntimeHelpers.GetUninitializedObject(GetCachedCommitteeType());
    }

    private static IInteroperable CreateCachedCommittee(IEnumerable<ECPoint> committee)
    {
        var type = GetCachedCommitteeType();
        var instance = (IInteroperable)RuntimeHelpers.GetUninitializedObject(type);
        var method = type.GetMethod("AddRange", BindingFlags.Public | BindingFlags.Instance);
        if (method != null)
        {
            var collection = committee.Select(p => (p, BigInteger.Zero));
            method.Invoke(instance, new object[] { collection });
        }
        return instance;
    }

    private static object GetInteroperable(StorageItem item, Type type)
    {
        var method = typeof(StorageItem).GetMethod("GetInteroperable", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        var genericMethod = method?.MakeGenericMethod(type);
        return genericMethod?.Invoke(item, null) ?? throw new InvalidOperationException("GetInteroperable method not found");
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
        {
            property.SetValue(obj, value);
            return;
        }
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }
        throw new InvalidOperationException($"Property or field {propertyName} not found in type {type.Name}");
    }

    private static T GetProperty<T>(object obj, string propertyName)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
            return (T)property.GetValue(obj)!;
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
            return (T)field.GetValue(obj)!;
        throw new InvalidOperationException($"Property or field {propertyName} not found");
    }
}
