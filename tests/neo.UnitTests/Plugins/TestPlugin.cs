using Microsoft.Extensions.Configuration;
using Neo.Plugins;

namespace Neo.UnitTests.Plugins
{
    public class TestPlugin : Plugin
    {
        public TestPlugin() : base() { }

        protected override void Configure() { }

        public void LogMessage(string message)
        {
            Log(message);
        }

        public bool TestOnMessage(object message)
        {
            return OnMessage(message);
        }

        public IConfigurationSection TestGetConfiguration()
        {
            return GetConfiguration();
        }

        protected override bool OnMessage(object message) => true;
    }
}
