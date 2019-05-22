using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;
using System.Reflection;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NativeContractPrices
    {
        [TestMethod]
        public void AllNativeContractPriceAreSet()
        {
            Type t = typeof(NativeContract);
            foreach (Type ct in t.Assembly.GetTypes())
            {
                if (t.IsAssignableFrom(ct))
                {
                    MethodInfo method1 = ct.GetMethod("GetPrice", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo method2 = ct.GetMethod("GetPriceForMethod", BindingFlags.NonPublic | BindingFlags.Instance);
                    (method1.DeclaringType == ct || method2.DeclaringType == ct).Should().BeTrue();
                }
            }
        }
    }
}
