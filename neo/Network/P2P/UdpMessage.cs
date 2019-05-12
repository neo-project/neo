using Akka.IO;
using System.Net;

namespace Neo.Network.P2P
{
    public class UdpMessage
    {
        /// <summary>
        /// Sender
        /// </summary>
        public IPEndPoint Sender { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public ByteString Data { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public UdpMessage() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="data">Data</param>
        public UdpMessage(IPEndPoint sender, ByteString data)
        {
            Sender = sender;
            Data = data;
        }
    }
}