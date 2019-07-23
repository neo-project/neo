using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                               where t.GetCustomAttribute<TestClassAttribute>() != null && t.GetCustomAttribute<NotReRunnableAttribute>() == null
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
            var emptyObjArray = new object[] { };
            var testContext = new object[] { new UnitTestContext() };

            // run all the tests, varying the culture each time.
            try
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);

                    foreach (var c in testClasses)
                    {
                        var instance = c.Constructor.Invoke(emptyObjArray);
                        if (c.ClassInit != null)
                        {
                            c.ClassInit.Invoke(instance, testContext);
                        }
                        foreach (var m in c.TestMethods)
                        {
                            if (c.TestInit != null)
                            {
                                c.TestInit.Invoke(instance, emptyObjArray);
                            }
                            m.Invoke(instance, emptyObjArray);
                            if (c.TestCleanup != null)
                            {
                                c.TestCleanup.Invoke(instance, emptyObjArray);
                            }
                        }
                        if (c.ClassCleanup != null)
                        {
                            c.ClassCleanup.Invoke(instance, emptyObjArray);
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

    public class UnitTestContext : TestContext
    {
        public override IDictionary<string, object> Properties => throw new NotImplementedException();

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public override void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class NotReRunnableAttribute : Attribute
    {

    }
}
