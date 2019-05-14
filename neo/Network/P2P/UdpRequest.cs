using System.Net;

namespace Neo.Network.P2P
{
    public class UdpRequest
    {
        /// <summary>
        /// Sender
        /// </summary>
        public IPEndPoint Sender { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public Message Message { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public UdpRequest() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="message">Message</param>
        public UdpRequest(IPEndPoint sender, Message message)
        {
            Sender = sender;
            Message = message;
        }
    }
}