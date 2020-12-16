using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public class ContractState : IInteroperable
    {
        public int Id;
        public ushort UpdateCounter;
        public UInt160 Hash;
        public byte[] Script;
        public ContractManifest Manifest;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            Id = (int)array[0].GetInteger();
            UpdateCounter = (ushort)array[1].GetInteger();
            Hash = new UInt160(array[2].GetSpan());
            Script = array[3].GetSpan().ToArray();
            Manifest = ContractManifest.Parse(array[4].GetSpan());
        }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="targetContract">The contract that we are calling</param>
        /// <param name="targetMethod">The method that we are calling</param>
        /// <returns>Return true or false</returns>
        public bool CanCall(ContractState targetContract, string targetMethod)
        {
            return Manifest.Permissions.Any(u => u.IsAllowed(targetContract, targetMethod));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["id"] = Id;
            json["updatecounter"] = UpdateCounter;
            json["hash"] = Hash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Id, (int)UpdateCounter, Hash.ToArray(), Script, Manifest.ToString() });
        }
    }
}
