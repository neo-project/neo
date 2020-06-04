using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor CreateCallback = Register("System.Callback.Create", nameof(Callback_Create), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor CreateFromSyscall = Register("System.Callback.CreateFromSyscall", nameof(Callback_CreateFromSyscall), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor InvokeCallback = Register("System.Callback.Invoke", nameof(Callback_Invoke), 0_00000400, TriggerType.All, CallFlags.None);

        internal void Callback_Invoke(Callback callback)
        {
            callback.Action(this);
        }

        internal void Callback_Create(Pointer pointer, int parcount)
        {
            // Save arguments

            var context = CurrentContext;
            var arguments = new StackItem[parcount];
            for (int x = parcount - 1; x >= 0; x--)
            {
                arguments[x] = Pop();
            }

            // Push callback

            Push(new InteropInterface(new Callback(engine =>
            {
                // Clone context

                var newContext = context.Clone(0);
                newContext.InstructionPointer = pointer.Position;

                // Copy arguments

                foreach (var arg in arguments)
                {
                    newContext.EvaluationStack.Push(arg);
                }

                // Load context

                LoadContext(newContext);
            },
            arguments)));
        }

        internal void Callback_CreateFromSyscall(uint method, int parcount)
        {
            // Save arguments

            var arguments = new StackItem[parcount];
            for (int x = parcount - 1; x >= 0; x--)
            {
                arguments[x] = Pop();
            }

            // Push callback

            Push(new InteropInterface(new Callback(engine =>
            {
                // Copy arguments

                foreach (var arg in arguments)
                {
                    Push(arg);
                }

                // Execute syscall

                OnSysCall(method);
            },
            arguments)));
        }

    }
}
