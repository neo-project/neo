// Copyright (C) 2015-2025 The Neo Project.
//
// ECPointTypeConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Neo.App.Configuration.Converters
{
    internal class ECPointTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string ecPointString)
            {
                if (ECPoint.TryParse(ecPointString, ECCurve.Secp256r1, out var ecPoint))
                    return ecPoint;

                throw new FormatException($"Invalid ECPoint format: {ecPointString}.");
            }

            return ECCurve.Secp256r1.G; // This is the default
        }
    }
}
