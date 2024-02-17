// Copyright (C) 2015-2024 The Neo Project.
//
// AnsiString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.CommandLine.Rendering
{
    internal sealed class AnsiString : IEnumerable<char>, IEnumerable, ICloneable, IComparable, IComparable<string>, IConvertible, IEquatable<string>
    {
        public int Length => _inputText.Length;

        private readonly AnsiStringStyle _defaultTextStyle;
        private readonly string _inputText;

        public AnsiString(
            string value,
            AnsiStringStyle style)
        {
            _inputText = value;
            _defaultTextStyle = style;
        }

        public unsafe AnsiString(char* value, AnsiStringStyle style) :
            this(new string(value), style)
        { }

        public AnsiString(char[] value, AnsiStringStyle style) :
            this(new string(value), style)
        { }

        public AnsiString(ReadOnlySpan<char> value, AnsiStringStyle style) :
            this(new string(value), style)
        { }

        public unsafe AnsiString(sbyte* value, AnsiStringStyle style) :
            this(new string(value), style)
        { }

        public AnsiString(char c, int count, AnsiStringStyle style) :
            this(new string(c, count), style)
        { }

        public unsafe AnsiString(char* value, int startIndex, int length, AnsiStringStyle style) :
            this(new string(value, startIndex, length), style)
        { }

        public AnsiString(char[] value, int startIndex, int length, AnsiStringStyle style) :
            this(new string(value, startIndex, length), style)
        { }

        public unsafe AnsiString(sbyte* value, int startIndex, int length, AnsiStringStyle style) :
            this(new string(value, startIndex, length), style)
        { }

        public char this[int index]
        {
            get => _inputText[index];
        }

        public static string Format(string format, params AnsiString?[] args) =>
            string.Format(format, args);

        public static string Format(AnsiStringStyle formatStyle, string format, params AnsiString?[] args) =>
            new AnsiString(string.Format(format, args), formatStyle);

        public static string Format(IFormatProvider provider, AnsiStringStyle formatStyle, string format, params AnsiString?[] args) =>
            new AnsiString(string.Format(provider, format, args), formatStyle);

        public CharEnumerator GetEnumerator() =>
            _inputText.GetEnumerator();

        IEnumerator<char> IEnumerable<char>.GetEnumerator() =>
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        object ICloneable.Clone() =>
            _inputText.Clone();

        int IComparable.CompareTo(object? obj) =>
            _inputText.CompareTo(obj);

        int IComparable<string>.CompareTo(string? other) =>
            _inputText.CompareTo(other);

        TypeCode IConvertible.GetTypeCode() =>
            _inputText.GetTypeCode();

        bool IConvertible.ToBoolean(IFormatProvider? provider) =>
            Convert.ToBoolean(_inputText, provider);

        byte IConvertible.ToByte(IFormatProvider? provider) =>
            Convert.ToByte(_inputText, provider);

        char IConvertible.ToChar(IFormatProvider? provider) =>
            Convert.ToChar(_inputText, provider);

        DateTime IConvertible.ToDateTime(IFormatProvider? provider) =>
            Convert.ToDateTime(_inputText, provider);

        decimal IConvertible.ToDecimal(IFormatProvider? provider) =>
            Convert.ToDecimal(_inputText, provider);

        double IConvertible.ToDouble(IFormatProvider? provider) =>
            Convert.ToDouble(_inputText, provider);

        short IConvertible.ToInt16(IFormatProvider? provider) =>
            Convert.ToInt16(_inputText, provider);

        int IConvertible.ToInt32(IFormatProvider? provider) =>
            Convert.ToInt32(_inputText, provider);

        long IConvertible.ToInt64(IFormatProvider? provider) =>
            Convert.ToInt64(_inputText, provider);

        sbyte IConvertible.ToSByte(IFormatProvider? provider) =>
            Convert.ToSByte(_inputText, provider);

        float IConvertible.ToSingle(IFormatProvider? provider) =>
            Convert.ToSingle(_inputText, provider);

        string IConvertible.ToString(IFormatProvider? provider) =>
            Convert.ToString(_inputText, provider);

        object IConvertible.ToType(Type conversionType, IFormatProvider? provider) =>
            Convert.ChangeType(_inputText, conversionType, provider);

        ushort IConvertible.ToUInt16(IFormatProvider? provider) =>
            Convert.ToUInt16(_inputText, provider);

        uint IConvertible.ToUInt32(IFormatProvider? provider) =>
            Convert.ToUInt32(_inputText, provider);

        ulong IConvertible.ToUInt64(IFormatProvider? provider) =>
            Convert.ToUInt64(_inputText, provider);

        bool IEquatable<string>.Equals(string? other) =>
            Equals(other);

        public override bool Equals(object? obj) =>
            ReferenceEquals(_inputText, obj) ?
            true :
            _inputText.Equals(obj);

        public override string ToString() =>
            $"\x1b[{_defaultTextStyle.Style};{_defaultTextStyle.Color};{_defaultTextStyle.Background}m{_inputText}\x1b[0m";

        public override int GetHashCode() =>
            _inputText.GetHashCode();

        public static bool operator ==(AnsiString a, AnsiString? b) =>
            a.Equals(b);

        public static bool operator !=(AnsiString a, AnsiString? b) =>
            !a.Equals(b);

        public static bool operator ==(AnsiString a, string? b) =>
            a.Equals(b);

        public static bool operator !=(AnsiString a, string? b) =>
            !a.Equals(b);

        public static bool operator ==(AnsiString a, char[] b) =>
            a.Equals(new string(b));

        public static bool operator !=(AnsiString a, char[] b) =>
            !a.Equals(new string(b));

        public unsafe static bool operator ==(AnsiString a, char* b) =>
            a.Equals(new string(b));

        public unsafe static bool operator !=(AnsiString a, char* b) =>
            !a.Equals(new string(b));

        public unsafe static bool operator ==(AnsiString a, sbyte* b) =>
            a.Equals(new string(b));

        public unsafe static bool operator !=(AnsiString a, sbyte* b) =>
            !a.Equals(new string(b));

        public static bool operator ==(AnsiString a, ReadOnlySpan<char> b) =>
            a.Equals(new string(b));

        public static bool operator !=(AnsiString a, ReadOnlySpan<char> b) =>
            !a.Equals(new string(b));


        public static implicit operator string(AnsiString value) =>
            $"{value}";

        public static implicit operator ReadOnlySpan<char>(AnsiString value) =>
            $"{value}";

    }
}
