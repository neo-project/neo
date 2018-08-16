using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public static class Helper
    {
        public static IConfigurationSection GetConfiguration(this Assembly assembly)
        {
            string path = Path.Combine("Plugins", assembly.GetName().Name, "config.json");
            return new ConfigurationBuilder().AddJsonFile(path).Build().GetSection("PluginConfiguration");
        }
    }
}
