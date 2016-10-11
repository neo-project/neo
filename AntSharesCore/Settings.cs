using Microsoft.Extensions.Configuration;
using System.Linq;

namespace AntShares
{
    internal class Settings
    {
        public uint Magic { get; private set; }
        public byte CoinVersion { get; private set; }
        public string[] StandbyMiners { get; private set; }
        public string[] SeedList { get; private set; }

        public static Settings Default { get; private set; }

        static Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("protocol.json").Build().GetSection("ProtocolConfiguration");
            Default = new Settings
            {
                Magic = uint.Parse(section.GetSection("Magic").Value),
                CoinVersion = byte.Parse(section.GetSection("CoinVersion").Value),
                StandbyMiners = section.GetSection("StandbyMiners").GetChildren().Select(p => p.Value).ToArray(),
                SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray()
            };
        }
    }
}
