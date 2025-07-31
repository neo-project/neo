using System;
using System.Text;

class Program
{
    static void Main()
    {
        // Test the hex conversion
        var hexWithPrefix = "0x48656c6c6f";
        var hexWithoutPrefix = hexWithPrefix.Substring(2);
        
        Console.WriteLine($"Hex with prefix: {hexWithPrefix}");
        Console.WriteLine($"Hex without prefix: {hexWithoutPrefix}");
        
        // Manual conversion
        var bytes = new byte[hexWithoutPrefix.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexWithoutPrefix.Substring(i * 2, 2), 16);
        }
        
        Console.WriteLine($"Bytes: {BitConverter.ToString(bytes)}");
        Console.WriteLine($"As UTF-8: {Encoding.UTF8.GetString(bytes)}");
        
        // Check if the hex string matches what we expect
        Console.WriteLine($"Expected: 48-65-6C-6C-6F");
        Console.WriteLine($"H = 0x48 = {(int)'H'}");
        Console.WriteLine($"e = 0x65 = {(int)'e'}");
        Console.WriteLine($"l = 0x6C = {(int)'l'}");
        Console.WriteLine($"l = 0x6C = {(int)'l'}");
        Console.WriteLine($"o = 0x6F = {(int)'o'}");
    }
}