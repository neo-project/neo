// Copyright (C) 2015-2024 The Neo Project.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        public string GetEngineErrorInfo()
        {
            if (State != VMState.FAULT || FaultException == null)
                return null;
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
            Exception baseException = FaultException.GetBaseException();
            traceback.AppendLine(baseException.StackTrace);
            traceback.AppendLine(baseException.Message);

            return traceback.ToString();
        }
    }
}
