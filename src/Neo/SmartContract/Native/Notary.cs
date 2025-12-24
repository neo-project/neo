// Copyright (C) 2015-2025 The Neo Project.
//
// Notary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native;

/// <summary>
/// The Notary native contract used for multisignature transactions forming assistance.
/// </summary>
public sealed class Notary : NativeContract
{
    /// <summary>
    /// A default value for maximum allowed NotValidBeforeDelta. It is set to be
    /// 20 rounds for 7 validators, a little more than half an hour for 15-seconds blocks.
    /// </summary>
    private const int DefaultMaxNotValidBeforeDelta = 140;
    /// <summary>
    /// A default value for deposit lock period.
    /// </summary>
    private const int DefaultDepositDeltaTill = 5760;
    private const byte Prefix_Deposit = 1;
    private const byte Prefix_MaxNotValidBeforeDelta = 10;

    internal Notary() : base(-10) { }

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_MaxNotValidBeforeDelta), new StorageItem(DefaultMaxNotValidBeforeDelta));
        }
        return ContractTask.CompletedTask;
    }

    internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        long nFees = 0;
        ECPoint[]? notaries = null;
        foreach (var tx in engine.PersistingBlock!.Transactions)
        {
            var attr = tx.GetAttribute<NotaryAssisted>();
            if (attr is not null)
            {
                notaries ??= GetNotaryNodes(engine.SnapshotCache);
                var nKeys = attr.NKeys;
                nFees += (long)nKeys + 1;
                if (tx.Sender == Hash)
                {
                    var payer = tx.Signers[1];
                    // Don't need to seal because Deposit is a fixed-sized interoperable, hence always can be serialized.
                    var balance = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Deposit, payer.Account))?.GetInteroperable<Deposit>();
                    if (balance != null)
                    {
                        balance.Amount -= tx.SystemFee + tx.NetworkFee;
                        if (balance.Amount.Sign == 0) RemoveDepositFor(engine.SnapshotCache, payer.Account);
                    }
                }
            }
        }
        if (nFees == 0) return;
        if (notaries == null) return;
        var singleReward = CalculateNotaryReward(engine.SnapshotCache, nFees, notaries.Length);
        foreach (var notary in notaries)
            await TokenManagement.MintInternal(engine, Governance.GasTokenId, Contract.CreateSignatureRedeemScript(notary).ToScriptHash(), singleReward, assertOwner: false, callOnPayment: false);
    }

    protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
    {
        manifest.SupportedStandards = ["NEP-27"];
    }

    /// <summary>
    /// Verify checks whether the transaction is signed by one of the notaries and
    /// ensures whether deposited amount of GAS is enough to pay the actual sender's fee.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="signature">Signature</param>
    /// <returns>Whether transaction is valid.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    private bool Verify(ApplicationEngine engine, byte[] signature)
    {
        if (signature is null || signature.Length != 64) return false;
        var tx = engine.ScriptContainer as Transaction;
        if (tx?.GetAttribute<NotaryAssisted>() is null) return false;
        foreach (var signer in tx.Signers)
        {
            if (signer.Account == Hash)
            {
                if (signer.Scopes != WitnessScope.None) return false;
                break;
            }
        }
        if (tx.Sender == Hash)
        {
            if (tx.Signers.Length != 2) return false;
            var payer = tx.Signers[1].Account;
            var balance = GetDepositFor(engine.SnapshotCache, payer);
            if (balance is null || balance.Amount.CompareTo(tx.NetworkFee + tx.SystemFee) < 0) return false;
        }
        var notaries = GetNotaryNodes(engine.SnapshotCache);
        var hash = tx.GetSignData(engine.GetNetwork());
        return notaries.Any(n => Crypto.VerifySignature(hash, signature, n));
    }

    /// <summary>
    /// OnNEP17Payment is a callback that accepts GAS transfer as Notary deposit.
    /// It also sets the deposit's lock height after which deposit can be withdrawn.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="assetId">Asset being sent (should be GAS)</param>
    /// <param name="from">GAS sender</param>
    /// <param name="amount">The amount of GAS sent</param>
    /// <param name="data">Deposit-related data: optional To value (treated as deposit owner if set) and Till height after which deposit can be withdrawn </param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void _OnPayment(ApplicationEngine engine, UInt160 assetId, UInt160 from, BigInteger amount, StackItem data)
    {
        if (assetId != Governance.GasTokenId) throw new InvalidOperationException(string.Format("only GAS can be accepted for deposit, got {0}", assetId));
        if (data is not Array additionalParams || additionalParams.Count != 2) throw new FormatException("`data` parameter should be an array of 2 elements");
        var to = from;
        if (!additionalParams[0].Equals(StackItem.Null)) to = additionalParams[0].GetSpan().ToArray().AsSerializable<UInt160>();
        var till = (uint)additionalParams[1].GetInteger();
        var tx = (Transaction)engine.ScriptContainer!;
        var allowedChangeTill = tx.Sender == to;
        var currentHeight = Ledger.CurrentIndex(engine.SnapshotCache);
        if (till < currentHeight + 2) throw new ArgumentOutOfRangeException(string.Format("`till` shouldn't be less than the chain's height {0} + 1", currentHeight + 2));
        // Don't need to seal because Deposit is a fixed-sized interoperable, hence always can be serialized.
        var deposit = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Deposit, to))?.GetInteroperable<Deposit>();
        if (deposit != null && till < deposit.Till) throw new ArgumentOutOfRangeException(string.Format("`till` shouldn't be less than the previous value {0}", deposit.Till));
        if (deposit is null)
        {
            var feePerKey = Policy.GetAttributeFee(engine.SnapshotCache, (byte)TransactionAttributeType.NotaryAssisted);
            if ((long)amount < 2 * feePerKey) throw new ArgumentOutOfRangeException(string.Format("first deposit can not be less than {0}, got {1}", 2 * feePerKey, amount));
            deposit = new Deposit() { Amount = 0, Till = 0 };
            if (!allowedChangeTill) till = currentHeight + DefaultDepositDeltaTill;
        }
        else if (!allowedChangeTill) till = deposit.Till;

        deposit.Amount += amount;
        deposit.Till = till;
        PutDepositFor(engine, to, deposit);
    }

    /// <summary>
    /// Lock asset until the specified height is unlocked.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="account">Account</param>
    /// <param name="till">specified height</param>
    /// <returns>Whether deposit lock height was successfully updated.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    public bool LockDepositUntil(ApplicationEngine engine, UInt160 account, uint till)
    {
        if (!engine.CheckWitnessInternal(account)) return false;
        if (till < Ledger.CurrentIndex(engine.SnapshotCache) + 2) return false; // deposit must be valid at least until the next block after persisting block.
        var deposit = GetDepositFor(engine.SnapshotCache, account);
        if (deposit is null || till < deposit.Till) return false;
        deposit.Till = till;

        PutDepositFor(engine, account, deposit);
        return true;
    }

    /// <summary>
    /// ExpirationOf returns deposit lock height for specified address.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <param name="account">Account</param>
    /// <returns>Deposit lock height of the specified address.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public uint ExpirationOf(DataCache snapshot, UInt160 account)
    {
        var deposit = GetDepositFor(snapshot, account);
        if (deposit is null) return 0;
        return deposit.Till;
    }

    /// <summary>
    /// BalanceOf returns deposited GAS amount for specified address.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <param name="account">Account</param>
    /// <returns>Deposit balance of the specified account.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public BigInteger BalanceOf(DataCache snapshot, UInt160 account)
    {
        var deposit = GetDepositFor(snapshot, account);
        if (deposit is null) return 0;
        return deposit.Amount;
    }

    /// <summary>
    /// Withdraw sends all deposited GAS for "from" address to "to" address. If "to"
    /// address is not specified, then "from" will be used as a sender.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="from">From Account</param>
    /// <param name="to">To Account</param>
    /// <returns>Whether withdrawal was successfull.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.All)]
    private async ContractTask<bool> Withdraw(ApplicationEngine engine, UInt160 from, UInt160? to)
    {
        if (!engine.CheckWitnessInternal(from)) return false;
        var receive = to is null ? from : to;
        var deposit = GetDepositFor(engine.SnapshotCache, from);
        if (deposit is null) return false;
        if (Ledger.CurrentIndex(engine.SnapshotCache) < deposit.Till) return false;
        RemoveDepositFor(engine.SnapshotCache, from);
        if (!await engine.CallFromNativeContractAsync<bool>(Hash, TokenManagement.Hash, "transfer", Governance.GasTokenId, Hash, receive, deposit.Amount, null))
        {
            throw new InvalidOperationException(string.Format("Transfer to {0} has failed", receive.ToString()));
        }
        return true;
    }

    /// <summary>
    /// GetMaxNotValidBeforeDelta is Notary contract method and returns the maximum NotValidBefore delta.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <returns>NotValidBefore</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public uint GetMaxNotValidBeforeDelta(IReadOnlyStore snapshot)
    {
        return (uint)(BigInteger)snapshot[CreateStorageKey(Prefix_MaxNotValidBeforeDelta)];
    }

    /// <summary>
    /// SetMaxNotValidBeforeDelta is Notary contract method and sets the maximum NotValidBefore delta.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="value">Value</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetMaxNotValidBeforeDelta(ApplicationEngine engine, uint value)
    {
        var maxVUBIncrement = engine.ProtocolSettings.MaxValidUntilBlockIncrement;
        if (value > maxVUBIncrement / 2 || value < ProtocolSettings.Default.ValidatorsCount)
        {
            throw new FormatException(string.Format("MaxNotValidBeforeDelta cannot be more than {0} or less than {1}",
               maxVUBIncrement / 2, ProtocolSettings.Default.ValidatorsCount));
        }
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_MaxNotValidBeforeDelta))!.Set(value);
    }

    /// <summary>
    /// GetNotaryNodes returns public keys of notary nodes.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <returns>Public keys of notary nodes.</returns>
    private static ECPoint[] GetNotaryNodes(DataCache snapshot)
    {
        return RoleManagement.GetDesignatedByRole(snapshot, Role.P2PNotary, Ledger.CurrentIndex(snapshot) + 1);
    }

    /// <summary>
    /// GetDepositFor returns Deposit for the specified account or nil in case if deposit
    /// is not found in storage.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="acc"></param>
    /// <returns>Deposit for the specified account.</returns>
    private Deposit? GetDepositFor(DataCache snapshot, UInt160 acc)
    {
        return snapshot.TryGet(CreateStorageKey(Prefix_Deposit, acc))?.GetInteroperable<Deposit>();
    }

    /// <summary>
    /// PutDepositFor puts deposit on the balance of the specified account in the storage.
    /// </summary>
    /// <param name="engine">ApplicationEngine</param>
    /// <param name="acc">Account</param>
    /// <param name="deposit">deposit</param>
    private void PutDepositFor(ApplicationEngine engine, UInt160 acc, Deposit deposit)
    {
        // Don't need to seal because Deposit is a fixed-sized interoperable, hence always can be serialized.
        var indeposit = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Deposit, acc), () => new StorageItem(deposit));
        indeposit!.Value = new StorageItem(deposit).Value;
    }

    /// <summary>
    /// RemoveDepositFor removes deposit from the storage.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <param name="acc">Account</param>
    private void RemoveDepositFor(DataCache snapshot, UInt160 acc)
    {
        snapshot.Delete(CreateStorageKey(Prefix_Deposit, acc));
    }

    /// <summary>
    /// CalculateNotaryReward calculates the reward for a single notary node based on FEE's count and Notary nodes count.
    /// </summary>
    /// <param name="snapshot">DataCache</param>
    /// <param name="nFees"></param>
    /// <param name="notariesCount"></param>
    /// <returns>result</returns>
    private static long CalculateNotaryReward(IReadOnlyStore snapshot, long nFees, int notariesCount)
    {
        return nFees * Policy.GetAttributeFee(snapshot, (byte)TransactionAttributeType.NotaryAssisted) / notariesCount;
    }

    public class Deposit : IInteroperable
    {
        public BigInteger Amount { get; set; }
        public uint Till { get; set; }

        public void FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Amount = @struct[0].GetInteger();
            Till = (uint)@struct[1].GetInteger();
        }

        public StackItem ToStackItem(IReferenceCounter? referenceCounter)
        {
            return new Struct(referenceCounter) { Amount, Till };
        }
    }
}
