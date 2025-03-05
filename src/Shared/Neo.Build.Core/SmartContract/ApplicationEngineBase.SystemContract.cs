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

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Logging;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.SmartContract;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual void SystemContractCall(UInt160 contractHash, string methodName, CallFlags callFlags, Array args)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} hash={Contract}, method={Method}, flags={Flags}, args={Args}",
                nameof(System_Contract_Call), contractHash, methodName, callFlags.ToString(), args.ToJson().ToString());

            CallContract(contractHash, methodName, callFlags, args);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Contract_Call), ResultStack.ToJson());
        }

        protected virtual void SystemContractCallNative(byte version)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} version=0x{Version}",
                nameof(System_Contract_CallNative), version.ToString("x02"));

            CallNativeContract(version);
        }

        protected virtual CallFlags SystemContractGetCallFlags()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Contract_GetCallFlags));

            var result = GetCallFlags();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Contract_GetCallFlags), result.ToString());

            return result;
        }

        protected virtual UInt160 SystemContractCreateStandardAccount(ECPoint publicKey)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} key={Key}",
                nameof(System_Contract_CreateStandardAccount), publicKey);

            var result = CreateStandardAccount(publicKey);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Contract_CreateStandardAccount), result);

            return result;
        }

        protected virtual UInt160 SystemContractCreateMultisigAccount(int verifyCount, ECPoint[] publicKeys)
        {
            var publicKeyStrings = publicKeys.Select(s => s.ToString());

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} m={Count}, keys=[{Keys}]",
                nameof(System_Contract_CreateMultisigAccount), verifyCount, string.Join(',', publicKeyStrings));

            var result = CreateMultisigAccount(verifyCount, publicKeys);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Contract_CreateMultisigAccount), result);

            return result;
        }

        protected virtual void SystemContractNativeOnPersist()
        {
            _traceLogger.LogInformation(VMEventLog.Persist,
                "{SysCall}",
                nameof(System_Contract_NativeOnPersist));

            NativeOnPersistAsync();
        }

        protected virtual void SystemContractNativePostPersist()
        {
            _traceLogger.LogInformation(VMEventLog.PostPersist,
                "{SysCall}",
                nameof(System_Contract_NativePostPersist));

            NativePostPersistAsync();
        }

    }
}
