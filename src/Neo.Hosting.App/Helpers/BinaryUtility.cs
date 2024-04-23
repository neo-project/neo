// Copyright (C) 2015-2024 The Neo Project.
//
// BinaryUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Hosting.App.Helpers
{
    internal static class BinaryUtility
    {
        public static long From7BitEncodedInt64(byte[] value, out int readBytes)
        {
            ulong result = 0;
            byte byteReadJustNow;

            var span = value.AsSpan();
            var pos = 0;
            readBytes = -1;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 9;
            for (var shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = span[pos++];
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    readBytes = pos;
                    return (long)result; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            byteReadJustNow = span[pos++];
            if (byteReadJustNow > 0b_1u)
            {
                throw new FormatException();
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            readBytes = pos;
            return (long)result;
        }

        public static byte[] To7BitEncodedInt64(long value)
        {
            byte[] buffer = [];
            var uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                buffer = [.. buffer, (byte)((uint)uValue | ~0x7Fu)];
                uValue >>= 7;
            }

            buffer = [.. buffer, (byte)uValue];

            return buffer;
        }

        public static int From7BitEncodedInt(byte[] value, out int readBytes)
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.

            uint result = 0;
            byte byteReadJustNow;

            var span = value.AsSpan();
            var pos = 0;
            readBytes = -1;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 4;
            for (var shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = span[pos++];
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    readBytes = pos;
                    return (int)result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = span[pos++];
            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException();
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            readBytes = pos;
            return (int)result;
        }

        public static byte[] To7BitEncodedInt(int value)
        {
            byte[] buffer = [];
            var uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                buffer = [.. buffer, (byte)(uValue | ~0x7Fu)];
                uValue >>= 7;
            }

            buffer = [.. buffer, (byte)uValue];

            return buffer;
        }
    }
}
