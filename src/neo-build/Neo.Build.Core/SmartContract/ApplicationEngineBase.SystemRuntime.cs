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
            var result = GetPlatform();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_Platform), result);

            return result;
        }

        protected virtual uint SystemRuntimeGetNetwork()
        {
            var result = GetNetwork();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetNetwork), result);

            return result;
        }

        protected virtual byte SystemRuntimeGetAddressVersion()
        {
            var result = GetAddressVersion();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result=0x{Result}",
                nameof(System_Runtime_GetAddressVersion), result.ToString("x02"));

            return result;
        }

        protected virtual TriggerType SystemRuntimeGetTrigger()
        {
            var result = Trigger;

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetTrigger), result.ToString());

            return result;
        }

        protected virtual ulong SystemRuntimeGetTime()
        {
            var result = GetTime();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetTime), result);

            return result;
        }

        protected virtual StackItem SystemRuntimeGetScriptContainer()
        {
            var result = GetScriptContainer();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetScriptContainer), result.ToJson());

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetExecutingScriptHash()
        {
            var result = CurrentScriptHash;

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetExecutingScriptHash), result);

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetCallingScriptHash()
        {
            var result = CallingScriptHash;

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetCallingScriptHash), result);

            return result;
        }

        protected virtual UInt160 SystemRuntimeGetEntryScriptHash()
        {
            var result = EntryScriptHash;

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetEntryScriptHash), result);

            return result;
        }

        protected virtual void SystemRuntimeLoadScript(byte[] script, CallFlags callFlags, Array args)
        {
            var scriptString = System.Convert.ToBase64String(script);

            RuntimeLoadScript(script, callFlags, args);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} script={Script}, flags={Flags}, args={Args}",
                nameof(System_Runtime_LoadScript), scriptString, callFlags.ToString(), args.ToJson());
        }

        protected virtual bool SystemRuntimeCheckWitness(byte[] scriptHashOrPublicKey)
        {
            var result = CheckWitness(scriptHashOrPublicKey);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} hash=0x{Hash}, result={Result}",
                nameof(System_Runtime_CheckWitness), scriptHashOrPublicKey.ToHexString(), result);

            return result;
        }

        protected virtual int SystemRuntimeGetInvocationCounter()
        {
            var result = GetInvocationCounter();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetInvocationCounter), result);

            return result;
        }

        protected virtual BigInteger SystemRuntimeGetRandom()
        {
            var result = GetRandom();

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GetRandom), result);

            return result;
        }

        protected virtual void SystemRuntimeLog(byte[] state)
        {
            RuntimeLog(state);

            _traceLogger.LogInformation(DebugEventLog.Log,
                "{SysCall} message={State}",
                nameof(System_Runtime_Log), _encoding.GetString(state));
        }

        protected virtual void SystemRuntimeNotify(byte[] eventName, Array state)
        {
            RuntimeNotify(eventName, state);

            _traceLogger.LogInformation(DebugEventLog.Notify,
                "{SysCall} event={Event}, state={State}",
                nameof(System_Runtime_Notify), eventName, state.ToJson());
        }

        protected virtual Array SystemRuntimeGetNotifications(UInt160 scriptHash)
        {
            var result = GetNotifications(scriptHash);

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} contract={Hash}, result={Result}",
                nameof(System_Runtime_GetNotifications), scriptHash, result.ToJson());

            return result;
        }

        protected virtual long SystemRuntimeGasLeft()
        {
            var result = GasLeft;

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Runtime_GasLeft), result);

            return result;
        }

        protected virtual void SystemRuntimeBurnGas(long datoshi)
        {
            BurnGas(datoshi);

            _traceLogger.LogInformation(DebugEventLog.Burn,
                "{SysCall} gas={Gas}",
                nameof(System_Runtime_BurnGas), datoshi);
        }

        protected virtual Signer[] SystemRuntimeCurrentSigners()
        {
            var result = GetCurrentSigners();

            var resultStrings = result.Select(s => s.ToJson());

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result=[{Result}]",
                nameof(System_Runtime_CurrentSigners), string.Join(',', resultStrings));

            return result;
        }
    }
}
