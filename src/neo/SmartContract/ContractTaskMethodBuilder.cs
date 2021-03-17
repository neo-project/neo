using System;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    sealed class ContractTaskMethodBuilder
    {
        private ContractTask task;

        public ContractTask Task => task ??= new ContractTask();

        public static ContractTaskMethodBuilder Create() => new();

        public void SetException(Exception exception)
        {
            Task.GetAwaiter().SetException(exception);
        }

        public void SetResult()
        {
            Task.GetAwaiter().SetResult();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }

    sealed class ContractTaskMethodBuilder<T>
    {
        private ContractTask<T> task;

        public ContractTask<T> Task => task ??= new ContractTask<T>();

        public static ContractTaskMethodBuilder<T> Create() => new();

        public void SetException(Exception exception)
        {
            Task.GetAwaiter().SetException(exception);
        }

        public void SetResult(T result)
        {
            Task.GetAwaiter().SetResult(result);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}
