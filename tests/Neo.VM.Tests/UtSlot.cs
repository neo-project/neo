using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtSlot
    {
        private static Slot CreateOrderedSlot(int count)
        {
            var check = new Integer[count];

            for (int x = 1; x <= count; x++)
            {
                check[x - 1] = x;
            }

            var slot = new Slot(check, new ReferenceCounter());

            Assert.AreEqual(count, slot.Count);
            CollectionAssert.AreEqual(check, slot.ToArray());

            return slot;
        }

        public static IEnumerable GetEnumerable(IEnumerator enumerator)
        {
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }

        [TestMethod]
        public void TestGet()
        {
            var slot = CreateOrderedSlot(3);

            Assert.IsTrue(slot[0] is Integer item0 && item0.Equals(1));
            Assert.IsTrue(slot[1] is Integer item1 && item1.Equals(2));
            Assert.IsTrue(slot[2] is Integer item2 && item2.Equals(3));
            Assert.ThrowsException<IndexOutOfRangeException>(() => slot[3] is Integer item3);
        }

        [TestMethod]
        public void TestEnumerable()
        {
            var slot = CreateOrderedSlot(3);

            BigInteger i = 1;
            foreach (Integer item in slot)
            {
                Assert.AreEqual(item.GetInteger(), i);
                i++;
            }

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, slot.ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)slot;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<Integer>().ToArray());

            Assert.AreEqual(3, slot.Count);

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, slot.ToArray());

            // Empty

            slot = CreateOrderedSlot(0);

            CollectionAssert.AreEqual(System.Array.Empty<Integer>(), slot.ToArray());

            // Test IEnumerable

            enumerable = (IEnumerable)slot;
            enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(System.Array.Empty<Integer>(), GetEnumerable(enumerator).Cast<Integer>().ToArray());

            Assert.AreEqual(0, slot.Count);

            CollectionAssert.AreEqual(System.Array.Empty<Integer>(), slot.ToArray());
        }
    }
}
