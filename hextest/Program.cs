using System;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    private static readonly Regex Base64Regex = new Regex(@"^[A-Za-z0-9+/]*={0,2}$", RegexOptions.Compiled);
    
    static byte[] ParseDataString(string data)
    {
        if (string.IsNullOrEmpty(data))
            return Array.Empty<byte>();

        Console.WriteLine($"Parsing: '{data}'");
        Console.WriteLine($"Length: {data.Length}, Length % 4 = {data.Length % 4}");
        Console.WriteLine($"Base64Regex.IsMatch: {Base64Regex.IsMatch(data)}");

        // Try Base64 first (most common case for backward compatibility)
        if (Base64Regex.IsMatch(data) && data.Length % 4 == 0)
        {
            try
            {
                var result = Convert.FromBase64String(data);
                Console.WriteLine("Parsed as Base64");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Base64 failed: {ex.Message}");
            }
        }

        // Try Hex with 0x prefix
        if (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var hexData = data.Substring(2);
                var bytes = new byte[hexData.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                }
                Console.WriteLine("Parsed as Hex with 0x prefix");
                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hex with prefix failed: {ex.Message}");
            }
        }

        // Try Hex without prefix (must be even length and all hex chars)
        if (data.Length % 2 == 0 && Regex.IsMatch(data, @"^[0-9A-Fa-f]+$"))
        {
            try
            {
                var bytes = new byte[data.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
                }
                Console.WriteLine("Parsed as Hex without prefix");
                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hex without prefix failed: {ex.Message}");
            }
        }

        // Fall back to UTF-8 encoding for Unicode strings
        Console.WriteLine("Parsed as UTF-8");
        return System.Text.Encoding.UTF8.GetBytes(data);
    }
    
    static void Main()
    {
        var testCases = new[] {
            "0x48656c6c6f",  // "Hello" in hex with prefix
            "48656c6c6f20576f726c64", // "Hello World" in hex without prefix
            "SGVsbG8gV29ybGQ=", // "Hello World" in Base64
            "你好世界",        // Unicode
            "ABCD"            // Ambiguous - could be hex or Base64
        };
        
        foreach (var test in testCases)
        {
            Console.WriteLine($"\n=== Testing: {test} ===");
            try
            {
                var bytes = ParseDataString(test);
                var asUtf8 = Encoding.UTF8.GetString(bytes);
                Console.WriteLine($"Bytes: {BitConverter.ToString(bytes)}");
                Console.WriteLine($"As UTF-8: {asUtf8}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}