// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using Neo.VM;

namespace Neo.SmartContract
{
    public class Diagnostic
    {
        public Tree<UInt160> InvocationTree { get; } = new();
        private TreeNode<UInt160> currentNodeOfInvocationTree = null;

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
    }
}
