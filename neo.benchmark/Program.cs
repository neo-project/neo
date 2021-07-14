using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace neo.benchmark
{

    public class Md5VsSha256
    {

        byte[] message = System.Text.Encoding.Default.GetBytes("hello");
        byte[] signature = "5331be791532d157df5b5620620d938bcb622ad02c81cfc184c460efdad18e695480d77440c511e9ad02ea30d773cb54e88f8cbb069644aefa283957085f38b5".HexToBytes();
        byte[] pubKey = "03ea01cb94bdaf0cd1c01b159d474f9604f4af35a3e2196f6bdfdb33b2aa4961fa".HexToBytes();

        [Benchmark]
        bool VerifySignature() => Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1);

    }



    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}

