using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    [AttributeUsage(AttributeTargets.Parameter)]
    abstract class ValidatorAttribute : Attribute
    {
        public abstract void Validate(StackItem item);
    }
}
