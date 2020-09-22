using System;
using System.IO;

namespace Neo.Models
{
    public class Witness
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        public virtual UInt160 ScriptHash => VerificationScript.ToScriptHash();
        public bool StateDependent => VerificationScript.Length == 0;
    }
}