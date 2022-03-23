using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Wallets;
using System;
using System.Linq;
using System.Runtime.InteropServices;
namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Bls12381
    {
        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_object_generator();

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g2_object_generator();

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern void test_generator_pairing(IntPtr result_test);

        private IntPtr g1ptr = IntPtr.Zero;
        private IntPtr g2ptr = IntPtr.Zero;

        [TestInitialize]
        public void TestSetup()
        {
            g1ptr = g1_object_generator();
            g2ptr = g2_object_generator();
        }

        [TestMethod]
        public void TestGtPairing()
        {
            IntPtr result_test = Bls12381.g1_g2_pairing(g1ptr, g2ptr);
            test_generator_pairing(result_test);
        }
    }
}
