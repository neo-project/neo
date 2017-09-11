using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Culture
    {
        // This test runs all the other unit tests in the project, with a variety of cultures
        // This test will fail when any other test in the project fails. Fix the other failing test(s) and this test should pass again.
        [TestMethod]
        [NotReRunnable]
        public void All_Tests_Cultures()
        {
            // get all tests in the unit test project assembly
            var testClasses = (from t in typeof(NotReRunnableAttribute).GetTypeInfo().Assembly.DefinedTypes
                               where t.GetCustomAttribute<TestClassAttribute>() != null
                               select new
                               {
                                   Constructor = t.GetConstructor(new Type[] { }),
                                   ClassInit = t.GetMethods().Where(
                                   m => m.GetCustomAttribute<ClassInitializeAttribute>() != null).SingleOrDefault(),
                                   TestInit = t.GetMethods().Where(
                                   m => m.GetCustomAttribute<TestInitializeAttribute>() != null).SingleOrDefault(),
                                   TestCleanup = t.GetMethods().Where(
                                   m => m.GetCustomAttribute<TestCleanupAttribute>() != null).SingleOrDefault(),
                                   ClassCleanup = t.GetMethods().Where(
                                   m => m.GetCustomAttribute<ClassCleanupAttribute>() != null).SingleOrDefault(),
                                   TestMethods = t.GetMethods().Where(
                                   m => m.GetCustomAttribute<TestMethodAttribute>() != null
                                   && m.GetCustomAttribute<NotReRunnableAttribute>() == null).ToList()
                               }).ToList();

            var cultures = new string[] { "en-US", "zh-CN", "de-DE", "ko-KR", "ja-JP" };
            var originalUICulture = CultureInfo.CurrentCulture;
            var emtpyObjArray = new object[] { };

            // run all the tests, varying the culture each time.
            try
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    foreach (var c in testClasses)
                    {
                        var instance = c.Constructor.Invoke(emtpyObjArray);
                        if (c.ClassInit != null)
                        {
                            c.ClassInit.Invoke(instance, emtpyObjArray);
                        }
                        foreach (var m in c.TestMethods)
                        {
                            if (c.TestInit != null)
                            {
                                c.TestInit.Invoke(instance, emtpyObjArray);
                            }
                            m.Invoke(instance, emtpyObjArray);
                            if (c.TestCleanup != null)
                            {
                                c.TestCleanup.Invoke(instance, emtpyObjArray);
                            }
                        }
                        if (c.ClassCleanup != null)
                        {
                            c.ClassCleanup.Invoke(instance, emtpyObjArray);
                        }
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalUICulture;
            }

        }
    }

    public class NotReRunnableAttribute : Attribute
    {

    }
}
