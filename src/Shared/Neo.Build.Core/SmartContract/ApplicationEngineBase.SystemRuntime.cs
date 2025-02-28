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

using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using StackItem = Neo.VM.Types.StackItem;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual string SystemRuntimePlatform()
        {
            return GetPlatform();
        }

        protected virtual uint SystemRuntimeGetNetwork()
        {
            return GetNetwork();
        }

        protected virtual byte SystemRuntimeGetAddressVersion()
        {
            return GetAddressVersion();
        }

        protected virtual TriggerType SystemRuntimeGetTrigger()
        {
            return Trigger;
        }

        protected virtual ulong SystemRuntimeGetTime()
        {
            return GetTime();
        }

        protected virtual StackItem SystemRuntimeGetScriptContainer()
        {
            return GetScriptContainer();
        }

        protected virtual UInt160 SystemRuntimeGetExecutingScriptHash()
        {
            return CurrentScriptHash;
        }

        protected virtual UInt160 SystemRuntimeGetCallingScriptHash()
        {
            return CallingScriptHash;
        }

        protected virtual UInt160 SystemRuntimeGetEntryScriptHash()
        {
            return EntryScriptHash;
        }

        protected virtual void SystemRuntimeLoadScript(byte[] script, CallFlags callFlags, Array args)
        {
            RuntimeLoadScript(script, callFlags, args);
        }

        protected virtual bool SystemRuntimeCheckWitness(byte[] scriptHashOrPublicKey)
        {
            return CheckWitness(scriptHashOrPublicKey);
        }

        protected virtual int SystemRuntimeGetInvocationCounter()
        {
            return GetInvocationCounter();
        }

        protected virtual BigInteger SystemRuntimeGetRandom()
        {
            return GetRandom();
        }

        protected internal void SystemRuntimeLog(byte[] state)
        {
            RuntimeLog(state);
        }

        protected internal void SystemRuntimeNotify(byte[] eventName, Array state)
        {
            RuntimeNotify(eventName, state);
        }

        protected virtual Array SystemRuntimeGetNotifications(UInt160 scriptHash)
        {
            return GetNotifications(scriptHash);
        }

        protected virtual long SystemRuntimeGasLeft()
        {
            return GasLeft;
        }

        protected virtual void SystemRuntimeBurnGas(long datoshi)
        {
            BurnGas(datoshi);
        }

        protected virtual Signer[] SystemRuntimeCurrentSigners()
        {
            return GetCurrentSigners();
        }
    }
}
