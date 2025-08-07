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

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} hash={Contract}, method={Method}, flags={Flags}, args={Args}, result={Result}",
                nameof(System_Contract_Call), contractHash, methodName, callFlags.ToString(), args.ToJson().ToString(), ResultStack.ToJson());
        }

        protected virtual void SystemContractCallNative(byte version)
        {
            CallNativeContract(version);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} version=0x{Version}",
                nameof(System_Contract_CallNative), version.ToString("x02"));
        }

        protected virtual CallFlags SystemContractGetCallFlags()
        {
            var result = GetCallFlags();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Contract_GetCallFlags), result.ToString());

            return result;
        }

        protected virtual UInt160 SystemContractCreateStandardAccount(ECPoint publicKey)
        {
            var result = CreateStandardAccount(publicKey);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} key={Key}, result={Result}",
                nameof(System_Contract_CreateStandardAccount), publicKey, result);

            return result;
        }

        protected virtual UInt160 SystemContractCreateMultisigAccount(int verifyCount, ECPoint[] publicKeys)
        {
            var publicKeyStrings = publicKeys.Select(s => s.ToString());
            var result = CreateMultisigAccount(verifyCount, publicKeys);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} m={Count}, keys=[{Keys}], result={Result}",
                nameof(System_Contract_CreateMultisigAccount), verifyCount, string.Join(',', publicKeyStrings), result);

            return result;
        }

        protected virtual void SystemContractNativeOnPersist()
        {
            NativeOnPersistAsync();

            _traceLogger.LogInformation(DebugEventLog.Persist,
                "{SysCall}",
                nameof(System_Contract_NativeOnPersist));
        }

        protected virtual void SystemContractNativePostPersist()
        {
            NativePostPersistAsync();

            _traceLogger.LogInformation(DebugEventLog.PostPersist,
                "{SysCall}",
                nameof(System_Contract_NativePostPersist));
        }
    }
}
