// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ReflectionCacheAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_ReflectionCacheAttribute
    {
        [TestMethod]
        public void Constructor_StoresType()
        {
            var attr = new ReflectionCacheAttribute(typeof(string));
            Assert.AreEqual(typeof(string), attr.Type);
        }

        [TestMethod]
        public void AttributeUsage_IsFieldOnly()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(ReflectionCacheAttribute), typeof(AttributeUsageAttribute))!;
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Field, usage.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
        }
    }
}
