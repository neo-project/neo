// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.SystemContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Array = Neo.VM.Types.Array;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual void SystemContractCall(UInt160 contractHash, string method, CallFlags callFlags, Array args)
        {
            CallContract(contractHash, method, callFlags, args);
        }

        protected virtual void SystemContractCallNative(byte version)
        {
            CallNativeContract(version);
        }

        protected virtual CallFlags SystemContractGetCallFlags()
        {
            return GetCallFlags();
        }

        protected virtual UInt160 SystemContractCreateStandardAccount(ECPoint publicKey)
        {
            return CreateStandardAccount(publicKey);
        }

        protected virtual UInt160 SystemContractCreateMultisigAccount(int verifyCount, ECPoint[] publicKeys)
        {
            return CreateMultisigAccount(verifyCount, publicKeys);
        }

        protected virtual void SystemContractNativeOnPersist()
        {
            NativeOnPersistAsync();
        }

        protected internal void SystemContractNativePostPersist()
        {
            NativePostPersistAsync();
        }

    }
}
