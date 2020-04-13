using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class StateRootsPayload : ISerializable
    {
        public const int MaxStateRootsCount = 2000;

        public StateRoot[] StateRoots;

        public int Size => StateRoots.GetVarSize();

        public static StateRootsPayload Create(IEnumerable<StateRoot> state_roots)
        {
            return new StateRootsPayload
            {
                StateRoots = state_roots.ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StateRoots = reader.ReadSerializableArray<StateRoot>(MaxStateRootsCount);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateRoots);
        }
    }
}
