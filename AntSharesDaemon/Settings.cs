using Microsoft.Extensions.Configuration;
using System.Linq;

namespace AntShares
{
    internal class Settings
    {
        public string DataDirectoryPath { get; private set; }
        public ushort NodePort { get; private set; }
        public string[] UriPrefix { get; private set; }
        public string SslCert { get; private set; }
        public string SslCertPassword { get; private set; }

        public static Settings Default { get; private set; }

        static Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            Default = new Settings
            {
                DataDirectoryPath = section.GetSection("DataDirectoryPath").Value,
                NodePort = ushort.Parse(section.GetSection("NodePort").Value),
                UriPrefix = section.GetSection("UriPrefix").GetChildren().Select(p => p.Value).ToArray(),
                SslCert = section.GetSection("SslCert").Value,
                SslCertPassword = section.GetSection("SslCertPassword").Value
            };
        }
    }
}
