using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to respond to <see cref="MessageCommand.GetAddr"/> messages.
    /// </summary>
    public class AddrPayload : ISerializable
    {
        /// <summary>
        /// Indicates the maximum number of nodes sent each time.
        /// </summary>
        public const int MaxCountToSend = 200;

        /// <summary>
        /// The list of nodes.
        /// </summary>
        public NetworkAddressWithTime[] AddressList;

        public int Size => AddressList.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="AddrPayload"/> class.
        /// </summary>
        /// <param name="addresses">The list of nodes.</param>
        /// <returns>The created payload.</returns>
        public static AddrPayload Create(params NetworkAddressWithTime[] addresses)
        {
            return new AddrPayload
            {
                AddressList = addresses
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            AddressList = reader.ReadSerializableArray<NetworkAddressWithTime>(MaxCountToSend);
            if (AddressList.Length == 0)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(AddressList);
        }
    }
}
