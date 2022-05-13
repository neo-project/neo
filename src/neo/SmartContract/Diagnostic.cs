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
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public class Diagnostic
    {
        public Tree<UInt160> InvocationTree { get; } = new();
        public List<NotifyEventArgs> Notifications { get; } = new();
        public List<LogEventArgs> Logs { get; } = new();

        private ApplicationEngine engine;
        private TreeNode<UInt160> currentNodeOfInvocationTree;

        internal void Initialized(ApplicationEngine engine)
        {
            this.engine = engine;
            ApplicationEngine.Notify += ApplicationEngine_Notify;
            ApplicationEngine.Log += ApplicationEngine_Log;
        }

        internal void Dispose()
        {
            ApplicationEngine.Notify -= ApplicationEngine_Notify;
            ApplicationEngine.Log -= ApplicationEngine_Log;
        }

        internal void ContextLoaded(ExecutionContext context)
        {
            var state = context.GetState<ExecutionContextState>();
            if (currentNodeOfInvocationTree is null)
                currentNodeOfInvocationTree = InvocationTree.AddRoot(state.ScriptHash);
            else
                currentNodeOfInvocationTree = currentNodeOfInvocationTree.AddChild(state.ScriptHash);
        }

        internal void ContextUnloaded(ExecutionContext context)
        {
            currentNodeOfInvocationTree = currentNodeOfInvocationTree.Parent;
        }

        private void ApplicationEngine_Notify(object sender, NotifyEventArgs e)
        {
            if (sender != engine) return;
            Notifications.Add(e);
        }

        private void ApplicationEngine_Log(object sender, LogEventArgs e)
        {
            if (sender != engine) return;
            Logs.Add(e);
        }
    }
}
