// Copyright (C) 2015-2024 The Neo Project.
//
// Diagnostic.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.VM;

namespace Neo.Plugins.RpcServer
{
    class Diagnostic : IDiagnostic
    {
        public Tree<UInt160> InvocationTree { get; } = new();

        private TreeNode<UInt160> currentNodeOfInvocationTree = null;

        public void Initialized(ApplicationEngine engine)
        {
        }

        public void Disposed()
        {
        }

        public void ContextLoaded(ExecutionContext context)
        {
            var state = context.GetState<ExecutionContextState>();
            if (currentNodeOfInvocationTree is null)
                currentNodeOfInvocationTree = InvocationTree.AddRoot(state.ScriptHash);
            else
                currentNodeOfInvocationTree = currentNodeOfInvocationTree.AddChild(state.ScriptHash);
        }

        public void ContextUnloaded(ExecutionContext context)
        {
            currentNodeOfInvocationTree = currentNodeOfInvocationTree.Parent;
        }

        public void PreExecuteInstruction(Instruction instruction)
        {
        }

        public void PostExecuteInstruction(Instruction instruction)
        {
        }
    }
}
