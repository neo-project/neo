// Copyright (C) 2015-2024 The Neo Project.
//
// SecureStringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Neo.Extensions
{
    public static class SecureStringExtensions
    {
        public static string? GetClearText(this SecureString secureString)
        {
            if (secureString is null)
                throw new ArgumentNullException(nameof(secureString));

            var unmanagedStringPtr = IntPtr.Zero;

            try
            {
                unmanagedStringPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedStringPtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedStringPtr);
            }
        }

        public static SecureString ToSecureString(this string value, bool asReadOnly = true)
        {
            unsafe
            {
                fixed (char* passwordChars = value)
                {
                    var securePasswordString = new SecureString(passwordChars, value.Length);

                    if (asReadOnly)
                        securePasswordString.MakeReadOnly();
                    return securePasswordString;
                }
            }
        }
    }
}
