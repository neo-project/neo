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
        public static string Reset = $"\x1b[{AnsiStyle.Default}m";

        public int Length => _inputText.Length;
        public int TrueLength => ToString().Length;

        private readonly AnsiStringStyle _defaultTextStyle;
        private string _inputText;

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
            set
            {
                var tmp = _inputText.ToCharArray();
                Array.Copy(new[] { value }, 0, tmp, index, 1);
                _inputText = new string(tmp);
            }
        }

        public char[] this[Range range]
        {
            get => _inputText.AsMemory()
                .Slice(range.Start.Value, range.Start.Value - range.End.Value)
                .ToArray();
            set
            {
                var tmp = _inputText.ToCharArray();
                Array.Copy(value, 0, tmp, range.Start.Value, value.Length);
                _inputText = new string(tmp);
            }
        }

        public static string Format(string format, params AnsiString?[] args) =>
            string.Format(format, args);

        public static string Format(AnsiStringStyle formatStyle, string format, params AnsiString?[] args) =>
            new AnsiString(string.Format(format, args), formatStyle);

        public static string Format(IFormatProvider provider, AnsiStringStyle formatStyle, string format, params AnsiString?[] args) =>
            new AnsiString(string.Format(provider, format, args), formatStyle);

        public IEnumerator<char> GetEnumerator() =>
            _inputText.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public object Clone() =>
            _inputText.Clone();

        public int CompareTo(object? obj) =>
            _inputText.CompareTo(obj);

        public int CompareTo(string? other) =>
            _inputText.CompareTo(other);

        public TypeCode GetTypeCode() =>
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

        public bool Equals(string? other) =>
            ReferenceEquals(_inputText, other) ?
            true :
            _inputText.Equals(other);

        public override bool Equals(object? obj) =>
            Equals(obj as string);

        public override string ToString() =>
            $"\x1b[{_defaultTextStyle.Style:d};{_defaultTextStyle.Color:d};{_defaultTextStyle.Background:d}m{_inputText}";

        public override int GetHashCode() =>
            _inputText.GetHashCode();

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
