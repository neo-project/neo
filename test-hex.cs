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

        // Try Base64 first (most common case for backward compatibility)
        if (Base64Regex.IsMatch(data) && data.Length % 4 == 0)
        {
            try
            {
                return Convert.FromBase64String(data);
            }
            catch
            {
                // Not valid Base64, continue to other formats
            }
        }

        // Try Hex with 0x prefix
        if (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var hexData = data.Substring(2);
                return Convert.FromHexString(hexData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hex with prefix failed: {ex.Message}");
                // Not valid hex, continue
            }
        }

        // Try Hex without prefix (must be even length and all hex chars)
        if (data.Length % 2 == 0 && Regex.IsMatch(data, @"^[0-9A-Fa-f]+$"))
        {
            try
            {
                return Convert.FromHexString(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hex without prefix failed: {ex.Message}");
                // Not valid hex, continue
            }
        }

        // Fall back to UTF-8 encoding for Unicode strings
        return System.Text.Encoding.UTF8.GetBytes(data);
    }
    
    static void Main()
    {
        // Test cases
        var testCases = new[] {
            "0x48656c6c6f",  // "Hello" in hex with prefix
            "48656c6c6f",     // "Hello" in hex without prefix
            "SGVsbG8gV29ybGQ=", // "Hello World" in Base64
            "你好世界",        // Unicode
            "ABCD"            // Ambiguous - could be hex or Base64
        };
        
        foreach (var test in testCases)
        {
            Console.WriteLine($"\nTesting: {test}");
            try
            {
                var bytes = ParseDataString(test);
                var asUtf8 = Encoding.UTF8.GetString(bytes);
                Console.WriteLine($"  Bytes: {BitConverter.ToString(bytes)}");
                Console.WriteLine($"  As UTF-8: {asUtf8}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }
    }
}