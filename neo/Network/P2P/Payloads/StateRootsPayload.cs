using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class StateRootsPayload : ISerializable
    {
        public const int MaxStateRootsCount = 200;

        public StateRoot[] StateRoots;

        public int Size => StateRoots.GetVarSize();

        public static IEnumerable<StateRootsPayload> Create(IEnumerable<StateRoot> state_roots)
        {
            while (state_roots.Any())
            {
                var payload = new StateRootsPayload
                {
                    StateRoots = state_roots.Take(MaxStateRootsCount).ToArray()
                };
                yield return payload;
                state_roots = state_roots.Skip(MaxStateRootsCount);
            }
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
