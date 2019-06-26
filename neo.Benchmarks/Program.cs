using System;
using BenchmarkDotNet.Running;

namespace Neo.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var type in new Type[]
            {
                typeof(BenchmarkUInt),
                typeof(BenchmarkUInt256)
            })
            {
                var summary = BenchmarkRunner.Run(type); 

                Console.WriteLine(type.Name);
                Console.ReadLine();
            }
        }
    }
}