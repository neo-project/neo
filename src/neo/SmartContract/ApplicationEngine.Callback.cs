using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor CreateCallback = Register("System.Runtime.CreateCallback", nameof(Runtime_CreateCallback), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor CreateSyscallCallback = Register("System.Runtime.CreateSyscallCallback", nameof(Runtime_CreateSyscallCallback), 0_00000400, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor InvokeCallback = Register("System.Runtime.InvokeCallback", nameof(Runtime_InvokeCallback), 0_00000400, TriggerType.All, CallFlags.None);

        internal void Runtime_InvokeCallback(Callback callback)
        {
            callback.Action(this);
        }

        internal void Runtime_CreateCallback(Pointer pointer, int parcount)
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
                // Clone context

                var newContext = CurrentContext.Clone(0);
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

        internal void Runtime_CreateSyscallCallback(uint method, int parcount)
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
