using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_FindAllPathMethod
    {
        [TestMethod]
        public void TestFindPath()
        {
            List<String> tempNodesList = new List<String>();
            tempNodesList.Add("0");
            tempNodesList.Add("1");
            tempNodesList.Add("2");
            tempNodesList.Add("3");
            tempNodesList.Add("4");
            Dictionary<String, int> tempVectorGraphic = new Dictionary<String, int>();
            tempVectorGraphic.Add("0,1", 1);
            tempVectorGraphic.Add("0,2", 1);
            tempVectorGraphic.Add("1,3", 1);
            tempVectorGraphic.Add("2,3", 1);
            tempVectorGraphic.Add("3,4", 1);
            FindAllPathMethod method = new FindAllPathMethod(tempNodesList, tempVectorGraphic);
            List<List<String>> result = method.FindPath("0", "4");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("0,1,3,4", string.Join(",", result[0]));
            Assert.AreEqual("0,2,3,4", string.Join(",", result[1]));
        }

        [TestMethod]
        public void TestFindAllPath()
        {
            List<String> tempNodesList = new List<String>();
            tempNodesList.Add("0");
            tempNodesList.Add("1");
            tempNodesList.Add("2");
            Dictionary<String, int> tempVectorGraphic = new Dictionary<String, int>();
            tempVectorGraphic.Add("0,1", 1);
            FindAllPathMethod method = new FindAllPathMethod(tempNodesList, tempVectorGraphic);
            List<List<String>> result = method.FindAllPath();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("0,1", string.Join(",", result[0]));
        }

        [TestMethod]
        public void TestPrintPath()
        {
            List<String> tempNodesList = new List<String>();
            tempNodesList.Add("0");
            tempNodesList.Add("1");
            tempNodesList.Add("2");
            tempNodesList.Add("3");
            tempNodesList.Add("4");
            Dictionary<String, int> tempVectorGraphic = new Dictionary<String, int>();
            tempVectorGraphic.Add("0,1", 1);
            tempVectorGraphic.Add("0,2", 1);
            tempVectorGraphic.Add("1,3", 1);
            tempVectorGraphic.Add("2,3", 1);
            tempVectorGraphic.Add("3,4", 1);
            FindAllPathMethod method = new FindAllPathMethod(tempNodesList, tempVectorGraphic);
            Action action1 = () => method.PrintEdgeArray();
            action1.ShouldNotThrow<Exception>();
            Action action2 = () => method.PrintPath("0", "4");
            action2.ShouldNotThrow<Exception>();
            Action action3 = () => method.PrintAllPath();
            action3.ShouldNotThrow<Exception>();
        }
    }
}
