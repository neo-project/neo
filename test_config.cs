using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        // Test 1: Boolean serialized by System.Text.Json
        var config1 = new { PluginConfiguration = new { Enabled = false } };
        var json1 = JsonSerializer.Serialize(config1);
        Console.WriteLine($"JSON from System.Text.Json: {json1}");
        
        var tempFile1 = Path.GetTempFileName();
        File.WriteAllText(tempFile1, json1);
        
        var configuration1 = new ConfigurationBuilder()
            .AddJsonFile(tempFile1, optional: true)
            .Build()
            .GetSection("PluginConfiguration");
            
        var value1 = configuration1.GetValue<bool>("Enabled", true);
        Console.WriteLine($"GetValue<bool> result: {value1}");
        Console.WriteLine($"Raw value from config: '{configuration1["Enabled"]}'");
        
        // Test 2: Manually written JSON with boolean
        var json2 = @"{""PluginConfiguration"":{""Enabled"":false}}";
        Console.WriteLine($"\nManually written JSON: {json2}");
        
        var tempFile2 = Path.GetTempFileName();
        File.WriteAllText(tempFile2, json2);
        
        var configuration2 = new ConfigurationBuilder()
            .AddJsonFile(tempFile2, optional: true)  
            .Build()
            .GetSection("PluginConfiguration");
            
        var value2 = configuration2.GetValue<bool>("Enabled", true);
        Console.WriteLine($"GetValue<bool> result: {value2}");
        Console.WriteLine($"Raw value from config: '{configuration2["Enabled"]}'");
        
        // Cleanup
        File.Delete(tempFile1);
        File.Delete(tempFile2);
    }
}