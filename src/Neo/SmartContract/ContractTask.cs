// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder))]
    class ContractTask
    {
        private readonly ContractTaskAwaiter awaiter;

        public static ContractTask CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask();
            CompletedTask.GetAwaiter().SetResult();
        }

        public ContractTask()
        {
            awaiter = CreateAwaiter();
        }

        protected virtual ContractTaskAwaiter CreateAwaiter() => new();

        public virtual ContractTaskAwaiter GetAwaiter() => awaiter;

        public virtual object GetResult() => null;
    }

    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder<>))]
    class ContractTask<T> : ContractTask
    {
        protected override ContractTaskAwaiter<T> CreateAwaiter() => new();

        public override ContractTaskAwaiter<T> GetAwaiter() => (ContractTaskAwaiter<T>)base.GetAwaiter();

        public override object GetResult() => GetAwaiter().GetResult();
    }
}
