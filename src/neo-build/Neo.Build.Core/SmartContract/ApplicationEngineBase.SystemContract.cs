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
            CallContract(contractHash, methodName, callFlags, args);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Hash=\"{Contract}\" Method=\"{Method}\" Flags=\"{Flags}\" Args={Args}",
                nameof(System_Contract_Call), contractHash, methodName, callFlags.ToString(), args.ToJson().ToString());
        }

        protected virtual void SystemContractCallNative(byte version)
        {
            CallNativeContract(version);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Version=\"0x{Version}\"",
                nameof(System_Contract_CallNative), version.ToString("x02"));
        }

        protected virtual CallFlags SystemContractGetCallFlags()
        {
            var result = GetCallFlags();

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Result=\"{Result}\"",
                nameof(System_Contract_GetCallFlags), result.ToString());

            return result;
        }

        protected virtual UInt160 SystemContractCreateStandardAccount(ECPoint publicKey)
        {
            var result = CreateStandardAccount(publicKey);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Key=\"{Key}\" Result=\"{Result}\"",
                nameof(System_Contract_CreateStandardAccount), publicKey, result);

            return result;
        }

        protected virtual UInt160 SystemContractCreateMultisigAccount(int verifyCount, ECPoint[] publicKeys)
        {
            var publicKeyStrings = publicKeys.Select(s => s.ToString());
            var result = CreateMultisigAccount(verifyCount, publicKeys);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Count=\"{Count}\" Keys=\"[{Keys}]\" Result=\"{Result}\"",
                nameof(System_Contract_CreateMultisigAccount), verifyCount, string.Join(',', publicKeyStrings), result);

            return result;
        }

        protected virtual void SystemContractNativeOnPersist()
        {
            NativeOnPersistAsync();

            _traceLogger.LogDebug(DebugEventLog.Persist,
                "{SysCall}",
                nameof(System_Contract_NativeOnPersist));
        }

        protected virtual void SystemContractNativePostPersist()
        {
            NativePostPersistAsync();

            _traceLogger.LogDebug(DebugEventLog.PostPersist,
                "{SysCall}",
                nameof(System_Contract_NativePostPersist));
        }
    }
}
