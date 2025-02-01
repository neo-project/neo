// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;
using System.Text;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        public string GetEngineStackInfoOnFault(bool exceptionStackTrace = true, bool exceptionMessage = true)
        {
            if (State != VMState.FAULT || FaultException == null)
                return "";
            StringBuilder traceback = new();
            if (CallingScriptHash != null)
                traceback.AppendLine($"CallingScriptHash={CallingScriptHash}[{NativeContract.ContractManagement.GetContract(SnapshotCache, CallingScriptHash)?.Manifest.Name}]");
            traceback.AppendLine($"CurrentScriptHash={CurrentScriptHash}[{NativeContract.ContractManagement.GetContract(SnapshotCache, CurrentScriptHash)?.Manifest.Name}]");
            traceback.AppendLine($"EntryScriptHash={EntryScriptHash}");

            foreach (ExecutionContext context in InvocationStack.Reverse())
            {
                UInt160 contextScriptHash = context.GetScriptHash();
                string contextContractName = NativeContract.ContractManagement.GetContract(SnapshotCache, contextScriptHash)?.Manifest.Name;
                traceback.AppendLine($"\tInstructionPointer={context.InstructionPointer}, OpCode {context.CurrentInstruction?.OpCode}, Script Length={context.Script.Length} {contextScriptHash}[{contextContractName}]");
            }
            traceback.Append(GetEngineExceptionInfo(exceptionStackTrace: exceptionStackTrace, exceptionMessage: exceptionMessage));

            return traceback.ToString();
        }

        public string GetEngineExceptionInfo(bool exceptionStackTrace = true, bool exceptionMessage = true)
        {
            if (State != VMState.FAULT || FaultException == null)
                return "";
            StringBuilder traceback = new();
            Exception baseException = FaultException.GetBaseException();
            if (exceptionStackTrace)
                traceback.AppendLine(baseException.StackTrace);
            if (exceptionMessage)
                traceback.AppendLine(baseException.Message);
            return traceback.ToString();
        }
    }
}
