using System.Net;

namespace Neo
{
    public class EndPointConfig
    {
        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; } = "0.0.0.0";

        /// <summary>
        /// Port
        /// </summary>
        public ushort Port { get; set; } = 0;

        /// <summary>
        /// Ip EndPoint
        /// </summary>
        public IPEndPoint EndPoint => new IPEndPoint(IPAddress.Parse(Address), Port);

        /// <summary>
        /// Return true if the address and port are valid
        /// </summary>
        public bool IsValid => IPAddress.TryParse(Address, out var addr) && Port > 0;
    }
}