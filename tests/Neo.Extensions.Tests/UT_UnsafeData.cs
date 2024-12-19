using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_UnsafeData
    {
        [TestMethod]
        public void TestGetVarSizeInt()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == 0)
                {
                    int result = UnsafeData.GetVarSize(1);
                    int old = OldGetVarSize(1);
                    Assert.AreEqual(1, result);
                    Assert.AreEqual(1, old);
                }
                else if (i == 1)
                {
                    int result = UnsafeData.GetVarSize(ushort.MaxValue);
                    int old = OldGetVarSize(ushort.MaxValue);
                    Assert.AreEqual(3, result);
                    Assert.AreEqual(3, old);
                }
                else if (i == 2)
                {
                    int result = UnsafeData.GetVarSize(uint.MaxValue);
                    int old = OldGetVarSize(int.MaxValue);
                    Assert.AreEqual(5, result);
                    Assert.AreEqual(5, old);
                }
                else
                {
                    int result = UnsafeData.GetVarSize(long.MaxValue);
                    Assert.AreEqual(9, result);
                }
            }
        }

        [TestMethod]
        public void TestGetVarSizeGeneric()
        {
            for (int i = 0; i < 9; i++)
            {
                if (i == 0)
                {
                    int result = new UInt160[] { UInt160.Zero }.GetVarSize();
                    Assert.AreEqual(21, result);
                }
                else if (i == 1)//sbyte
                {
                    List<TestEnum0> initList = new()
                    {
                        TestEnum0.case1
                    };
                    IReadOnlyCollection<TestEnum0> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 2)//byte
                {
                    List<TestEnum1> initList = new()
                    {
                        TestEnum1.case1
                    };
                    IReadOnlyCollection<TestEnum1> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 3)//short
                {
                    List<TestEnum2> initList = new()
                    {
                        TestEnum2.case1
                    };
                    IReadOnlyCollection<TestEnum2> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 4)//ushort
                {
                    List<TestEnum3> initList = new()
                    {
                        TestEnum3.case1
                    };
                    IReadOnlyCollection<TestEnum3> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 5)//int
                {
                    List<TestEnum4> initList = new()
                    {
                        TestEnum4.case1
                    };
                    IReadOnlyCollection<TestEnum4> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 6)//uint
                {
                    List<TestEnum5> initList = new()
                    {
                        TestEnum5.case1
                    };
                    IReadOnlyCollection<TestEnum5> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 7)//long
                {
                    List<TestEnum6> initList = new()
                    {
                        TestEnum6.case1
                    };
                    IReadOnlyCollection<TestEnum6> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(9, result);
                }
                else if (i == 8)
                {
                    List<int> initList = new()
                    {
                        1
                    };
                    IReadOnlyCollection<int> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize<int>();
                    Assert.AreEqual(5, result);
                }
            }
        }

        enum TestEnum0 : sbyte
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum1 : byte
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum2 : short
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum3 : ushort
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum4 : int
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum5 : uint
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum6 : long
        {
            case1 = 1, case2 = 2
        }

        public static int OldGetVarSize(int value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= ushort.MaxValue)
                return sizeof(byte) + sizeof(ushort);
            else
                return sizeof(byte) + sizeof(uint);
        }
    }
}
