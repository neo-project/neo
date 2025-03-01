// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.SystemRuntime.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Logging;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using StackItem = Neo.VM.Types.StackItem;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual string SystemRuntimePlatform()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_Platform));

            var result = GetPlatform();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_Platform), result);

            return result;
        }

        protected virtual uint SystemRuntimeGetNetwork()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetNetwork));

            var result = GetNetwork();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetNetwork), result);

            return result;
        }

        protected virtual byte SystemRuntimeGetAddressVersion()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetAddressVersion));

            var result = GetAddressVersion();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result=0x{Result}",
                nameof(System_Runtime_GetAddressVersion), result.ToString("x02"));

            return result;
        }

        protected virtual TriggerType SystemRuntimeGetTrigger()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetTrigger));

            var result = Trigger;

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetTrigger), result.ToString());

            return result;
        }

        protected virtual ulong SystemRuntimeGetTime()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetTime));

            var result = GetTime();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetTime), result);

            return result;
        }

        protected virtual StackItem SystemRuntimeGetScriptContainer()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetScriptContainer));

            var result = GetScriptContainer();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetScriptContainer), result.ToJson());

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetExecutingScriptHash()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetExecutingScriptHash));

            var result = CurrentScriptHash;

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetExecutingScriptHash), result);

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetCallingScriptHash()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetCallingScriptHash));

            var result = CallingScriptHash;

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetCallingScriptHash), result);

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetEntryScriptHash()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetEntryScriptHash));

            var result = EntryScriptHash;

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetEntryScriptHash), result);

            return result;
        }

        protected virtual void SystemRuntimeLoadScript(byte[] script, CallFlags callFlags, Array args)
        {
            var scriptString = System.Convert.ToBase64String(script);

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} script={Script}, flags={Flags}, args={Args}",
                nameof(System_Runtime_LoadScript), scriptString, callFlags.ToString(), args.ToJson());

            RuntimeLoadScript(script, callFlags, args);
        }

        protected virtual bool SystemRuntimeCheckWitness(byte[] scriptHashOrPublicKey)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} hash=0x{Hash}",
                nameof(System_Runtime_CheckWitness), scriptHashOrPublicKey.ToHexString());

            var result = CheckWitness(scriptHashOrPublicKey);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_CheckWitness), result);

            return result;
        }

        protected virtual int SystemRuntimeGetInvocationCounter()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetInvocationCounter));

            var result = GetInvocationCounter();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetInvocationCounter), result);

            return result;
        }

        protected virtual BigInteger SystemRuntimeGetRandom()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GetRandom));

            var result = GetRandom();

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetRandom), result);

            return result;
        }

        protected internal void SystemRuntimeLog(byte[] state)
        {
            _traceLogger.LogInformation(VMEventLog.Log,
                "{SysCall} message={State}",
                nameof(System_Runtime_Log), _encoding.GetString(state));

            RuntimeLog(state);
        }

        protected internal void SystemRuntimeNotify(byte[] eventName, Array state)
        {
            _traceLogger.LogInformation(VMEventLog.Notify,
                "{SysCall} event={Event}, state={State}",
                nameof(System_Runtime_Notify), eventName, state.ToJson());

            RuntimeNotify(eventName, state);
        }

        protected virtual Array SystemRuntimeGetNotifications(UInt160 scriptHash)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} contract={Hash}",
                nameof(System_Runtime_GetNotifications), scriptHash);

            var result = GetNotifications(scriptHash);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetNotifications), result.ToJson());

            return result;
        }

        protected virtual long SystemRuntimeGasLeft()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_GasLeft));

            var result = GasLeft;

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GasLeft), result);

            return result;
        }

        protected virtual void SystemRuntimeBurnGas(long datoshi)
        {
            _traceLogger.LogInformation(VMEventLog.Burn,
                "{SysCall} gas={Gas}",
                nameof(System_Runtime_BurnGas), datoshi);

            BurnGas(datoshi);
        }

        protected virtual Signer[] SystemRuntimeCurrentSigners()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Runtime_CurrentSigners));

            var result = GetCurrentSigners();

            var resultStrings = result.Select(s => s.ToJson());

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result=[{Result}]",
                nameof(System_Runtime_CurrentSigners), string.Join(',', resultStrings));

            return result;
        }
    }
}
