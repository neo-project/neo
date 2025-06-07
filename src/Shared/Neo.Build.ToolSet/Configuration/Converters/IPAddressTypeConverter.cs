// Copyright (C) 2015-2025 The Neo Project.
//
// IPAddressTypeConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Neo.Build.ToolSet.Configuration.Converters
{
    internal class IPAddressTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string ipString)
            {
                if (IPAddress.TryParse(ipString, out var ipAddress))
                    return ipAddress;

                throw new FormatException($"Invalid IP address format: {ipString}");
            }

            throw new InvalidCastException();
        }
    }
}
