using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace TestApp
{
    public abstract class BasePlugin
    {
        public virtual string ConfigFile => Path.Combine(".", "config.json");
        
        protected IConfigurationSection GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile(ConfigFile, optional: true)
                .Build()
                .GetSection("PluginConfiguration");
        }
        
        protected virtual void Configure() { }
        
        public BasePlugin()
        {
            Configure();
        }
    }
    
    public class TestPlugin : BasePlugin
    {
        private bool _enabled = false;
        
        public bool IsEnabled => _enabled;
        
        protected override void Configure()
        {
            var config = GetConfiguration();
            _enabled = config.GetValue<bool>("Enabled", true);
            Console.WriteLine($"Configure called - Enabled value: {_enabled}");
            Console.WriteLine($"Raw config value: '{config["Enabled"]}'");
        }
    }
    
    public class TestablePlugin : TestPlugin
    {
        private string? _overrideConfigFile;
        
        public override string ConfigFile => _overrideConfigFile ?? base.ConfigFile;
        
        public void TestConfigure(string? configPath)
        {
            if (configPath != null)
            {
                _overrideConfigFile = configPath;
            }
            Configure();
        }
    }
    
    class Program
    {
        static void Main()
        {
            // Test with JSON file containing false
            var tempFile = Path.GetTempFileName();
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = false
            };
            
            var json = JsonSerializer.Serialize(new { PluginConfiguration = config });
            Console.WriteLine($"JSON content: {json}");
            File.WriteAllText(tempFile, json);
            
            // First test: using override approach
            Console.WriteLine("\nTest 1: Using override approach");
            var plugin1 = new TestablePlugin();
            plugin1.TestConfigure(tempFile);
            Console.WriteLine($"Plugin IsEnabled: {plugin1.IsEnabled}");
            
            // Second test: using reflection (what the test is trying to do)
            Console.WriteLine("\nTest 2: Using reflection approach");
            var plugin2 = new TestablePlugin();
            var configFileProperty = typeof(BasePlugin).GetProperty("ConfigFile", BindingFlags.Public | BindingFlags.Instance);
            Console.WriteLine($"ConfigFile property found: {configFileProperty != null}");
            Console.WriteLine($"Can write: {configFileProperty?.CanWrite}");
            
            // Try setting with reflection (this will fail because it's read-only)
            try
            {
                configFileProperty?.SetValue(plugin2, tempFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setting property failed: {ex.Message}");
            }
            
            // Cleanup
            File.Delete(tempFile);
        }
    }
}
