using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NEP17
{
    [DisplayName("SampleNep17Token")]
    [ManifestExtra("Author", "core-dev")]
    [ManifestExtra("Description", "A sample NEP-17 token")]
    [ManifestExtra("Email", "core@neo.org")]
    [ManifestExtra("Version", "0.0.1")]
    [ContractSourceCode("https://github.com/neo-project/neo/samples/NEP17")]
    [ContractPermission("*", "*")]
    [SupportedStandards("NEP-17")]
    public class SampleNep17Token : Nep17Token
    {
        #region Owner

        private const byte PrefixOwner = 0xff;

        [InitialValue("NUuJw4C4XJFzxAvSZnFTfsNoWZytmQKXQP", ContractParameterType.Hash160)]
        private static readonly UInt160 InitialOwner = default;

        [Safe]
        public static UInt160 GetOwner()
        {
            var currentOwner = Storage.Get(new[] { PrefixOwner });

            if (currentOwner == null)
                return InitialOwner;

            return (UInt160)currentOwner;
        }

        private static bool IsOwner() => Runtime.CheckWitness(GetOwner());

        public delegate void OnSetOwnerDelegate(UInt160 newOwner);

        [DisplayName("SetOwner")]
        public static event OnSetOwnerDelegate OnSetOwner;

        public static void SetOwner(UInt160 newOwner)
        {
            if (IsOwner() == false)
                throw new InvalidOperationException("No Authorization!");
            if (newOwner != null && newOwner.IsValid)
            {
                Storage.Put(new[] { PrefixOwner }, newOwner);
                OnSetOwner(newOwner);
            }
        }

        #endregion

        #region Minter

        private const byte PrefixMinter = 0xfd;

        [InitialValue("NUuJw4C4XJFzxAvSZnFTfsNoWZytmQKXQP", ContractParameterType.Hash160)]
        private static readonly UInt160 InitialMinter = default;

        [Safe]
        public static UInt160 GetMinter()
        {
            var currentMinter = Storage.Get(new[] { PrefixMinter });

            if (currentMinter == null)
                return InitialMinter;

            return (UInt160)currentMinter;
        }

        private static bool IsMinter() => Runtime.CheckWitness(GetMinter());

        public delegate void OnSetMinterDelegate(UInt160 newMinter);

        [DisplayName("SetMinter")]
        public static event OnSetMinterDelegate OnSetMinter;

        public static void SetMinter(UInt160 newMinter)
        {
            if (IsOwner() == false)
                throw new InvalidOperationException("No Authorization!");
            if (newMinter is not { IsValid: true }) return;
            Storage.Put(new[] { PrefixMinter }, newMinter);
            OnSetMinter(newMinter);
        }

        public new static void Mint(UInt160 to, BigInteger amount)
        {
            if (IsOwner() == false && IsMinter() == false)
                throw new InvalidOperationException("No Authorization!");
            Nep17Token.Mint(to, amount);
        }

        #endregion

        #region NEP17

        [Safe]
        public override byte Decimals() => 8;

        [Safe]
        public override string Symbol() => "SampleNep17Token";

        public new static void Burn(UInt160 account, BigInteger amount)
        {
            if (IsOwner() == false && IsMinter() == false)
                throw new InvalidOperationException("No Authorization!");
            Nep17Token.Burn(account, amount);
        }

        #endregion

        #region Payment

        public static bool Withdraw(UInt160 to, BigInteger amount)
        {
            if (IsOwner() == false)
                throw new InvalidOperationException("No Authorization!");
            if (amount <= 0)
                return false;
            // TODO: Add logic
            return true;
        }

        // NOTE: Allow NEP-17 tokens to be received for this contract
        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            // TODO: Add logic
        }

        #endregion

        #region Basic

        [Safe]
        public static bool Verify() => IsOwner();

        public static bool Update(ByteString nefFile, string manifest)
        {
            if (IsOwner() == false)
                throw new InvalidOperationException("No Authorization!");
            ContractManagement.Update(nefFile, manifest);
            return true;
        }

        #endregion
    }
}
