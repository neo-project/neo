using Neo.SmartContract.Callbacks;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Callback_Create = Register("System.Callback.Create", nameof(CreateCallback), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Callback_CreateFromSyscall = Register("System.Callback.CreateFromSyscall", nameof(CreateCallbackFromSyscall), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Callback_Invoke = Register("System.Callback.Invoke", nameof(InvokeCallback), 0_00000400, TriggerType.All, CallFlags.None);

        internal void InvokeCallback(CallbackBase callback, Array args)
        {
            //TODO: We should check the count of the return values

            if (args.Count != callback.ParametersCount)
                throw new InvalidOperationException();
            if (callback is PointerCallback pointerCallback)
                LoadContext(pointerCallback.GetContext());
            for (int i = args.Count - 1; i >= 0; i--)
                Push(args[i]);
            if (callback is SyscallCallback syscallCallback)
                OnSysCall(syscallCallback.Method);
        }

        internal PointerCallback CreateCallback(Pointer pointer, int parcount)
        {
            return new PointerCallback(CurrentContext, pointer, parcount);
        }

        internal SyscallCallback CreateCallbackFromSyscall(uint method)
        {
            return new SyscallCallback(method);
        }
    }
}
