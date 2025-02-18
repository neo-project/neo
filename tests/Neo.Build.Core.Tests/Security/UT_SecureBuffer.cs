// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SecureBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Security;
using System;
using System.Runtime.InteropServices;

namespace Neo.Build.Core.Tests.Security
{
    [TestClass]
    public class UT_SecureBuffer
    {
        [TestMethod]
        public void DidBufferGrow()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];
            var expectedLength = expectedMessage.Length;

            var buffer = new SecureBuffer(0);
            var actualPreLength = buffer.Length;

            buffer.Append(expectedMessage);

            var actualPostLength = buffer.Length;

            Assert.AreEqual(0, actualPreLength);
            Assert.AreEqual(expectedLength, actualPostLength);
        }

        [TestMethod]
        public unsafe void DidBufferDecrypt()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append(expectedMessage);

            Assert.AreEqual(expectedMessage.Length, buffer.Length);

            var ptrToMessage = buffer.MarshalToByteArray(true, decrypt: true);
            var actualMessage = new Span<byte>((void*)ptrToMessage, buffer.Length);

            CollectionAssert.AreEqual(expectedMessage, actualMessage.ToArray());

            Marshal.FreeHGlobal(ptrToMessage);
        }

        [TestMethod]
        public unsafe void DidBufferEncrypt()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append(expectedMessage);

            Assert.AreEqual(expectedMessage.Length, buffer.Length);

            var ptrToMessage = buffer.MarshalToByteArray(true, decrypt: false);
            var actualMessage = new Span<byte>((void*)ptrToMessage, buffer.Length);

            Assert.AreEqual(expectedMessage.Length, actualMessage.Length);
            CollectionAssert.AreNotEqual(expectedMessage, actualMessage.ToArray());

            Marshal.FreeHGlobal(ptrToMessage);
        }

        [TestMethod]
        public unsafe void DidDeepCopy()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append(expectedMessage);

            var actualBuffer = buffer.DeepCopy();

            Assert.AreEqual(expectedMessage.Length, actualBuffer.Length);

            var ptrToMessage = actualBuffer.MarshalToByteArray(true, decrypt: true);
            var actualMessage = new Span<byte>((void*)ptrToMessage, actualBuffer.Length);

            CollectionAssert.AreNotEqual(expectedMessage, actualMessage.ToArray());
        }

        [TestMethod]
        public unsafe void DidRemoveAt()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append([1, 2, 3, 4, 5, 6, 7, 8, 9, 0]);
            buffer.RemoveAt(5);

            Assert.AreEqual(expectedMessage.Length, buffer.Length);

            var ptrToMessage = buffer.MarshalToByteArray(true, decrypt: true);
            var actualMessage = new Span<byte>((void*)ptrToMessage, buffer.Length);

            CollectionAssert.AreEqual(expectedMessage, actualMessage.ToArray());
        }


        [TestMethod]
        public unsafe void DidInsertAt()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append([1, 2, 3, 4, 5, 7, 8, 9, 0]);
            buffer.InsertAt(5, 6);

            Assert.AreEqual(expectedMessage.Length, buffer.Length);

            var ptrToMessage = buffer.MarshalToByteArray(true, decrypt: true);
            var actualMessage = new Span<byte>((void*)ptrToMessage, buffer.Length);

            CollectionAssert.AreEqual(expectedMessage, actualMessage.ToArray());
        }

        [TestMethod]
        public unsafe void DidSetAt()
        {
            byte[] expectedMessage = [1, 2, 3, 4, 5, 0xff, 7, 8, 9, 0];

            var buffer = new SecureBuffer(0);
            buffer.Append([1, 2, 3, 4, 5, 6, 7, 8, 9, 0]);
            buffer.SetAt(5, 0xff);

            Assert.AreEqual(expectedMessage.Length, buffer.Length);

            var ptrToMessage = buffer.MarshalToByteArray(true, decrypt: true);
            var actualMessage = new Span<byte>((void*)ptrToMessage, buffer.Length);

            CollectionAssert.AreEqual(expectedMessage, actualMessage.ToArray());
        }
    }
}
