using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public class ContractState : IInteroperable
    {
        public int Id;
        public ushort UpdateCounter;
        public UInt160 Hash;
        public NefFile Nef;
        public ContractManifest Manifest;

        public byte[] Script => Nef.Script;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            Id = (int)array[0].GetInteger();
            UpdateCounter = (ushort)array[1].GetInteger();
            Hash = new UInt160(array[2].GetSpan());
            Nef = array[3].GetSpan().AsSerializable<NefFile>();
            Manifest = array[4].ToInteroperable<ContractManifest>();
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
            return new JObject
            {
                ["id"] = Id,
                ["updatecounter"] = UpdateCounter,
                ["hash"] = Hash.ToString(),
                ["nef"] = Nef.ToJson(),
                ["manifest"] = Manifest.ToJson()
            };
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Id, (int)UpdateCounter, Hash.ToArray(), Nef.ToArray(), Manifest.ToStackItem(referenceCounter) });
        }
    }
}
