using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Oracle
{
    partial class OracleContract
    {
        private class IdList : List<ulong>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (Array)stackItem)
                    Add(BinaryPrimitives.ReadUInt64LittleEndian(item.GetSpan()));
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Array(referenceCounter, this.Select(p => (StackItem)BitConverter.GetBytes(p)));
            }
        }

        private class NodeList : List<ECPoint>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (Array)stackItem)
                    Add(ECPoint.DecodePoint(item.GetSpan(), ECCurve.Secp256r1));
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Array(referenceCounter, this.Select(p => (StackItem)p.ToArray()));
            }
        }
    }
}
