using Neo.SmartContract.Callbacks;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Callback_Create = Register("System.Callback.Create", nameof(CreateCallback), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Callback_CreateFromSyscall = Register("System.Callback.CreateFromSyscall", nameof(CreateCallbackFromSyscall), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Callback_Invoke = Register("System.Callback.Invoke", nameof(InvokeCallback), 0_00000400, TriggerType.All, CallFlags.None);

        internal void InvokeCallback(CallbackBase callback)
        {
            callback.Action(this);
        }

        internal void CreateCallback(Pointer pointer, int parcount)
        {
            Push(new InteropInterface(new PointerCallback(CurrentContext, pointer, parcount)));
        }

        internal void CreateCallbackFromSyscall(uint method)
        {
            Push(new InteropInterface(new SyscallCallback(method)));
        }
    }
}
