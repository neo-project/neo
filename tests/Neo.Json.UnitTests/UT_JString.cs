// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JString
    {
        private static readonly JsonValue AsicString = JsonValue.Create("hello world");
        private static readonly JsonValue EscapeString = JsonValue.Create("\n\t\'\"");
        private static readonly JsonValue BadChar = JsonValue.Create(((char)0xff).ToString());
        private static readonly JsonValue IntegerString = JsonValue.Create("123");
        private static readonly JsonValue EmptyString = JsonValue.Create("");
        private static readonly JsonValue SpaceString = JsonValue.Create("    ");
        private static readonly JsonValue DoubleString = JsonValue.Create("123.456");
        private static readonly JsonValue UnicodeString = JsonValue.Create("\ud83d\ude03\ud83d\ude01");
        private static readonly JsonValue EmojString = JsonValue.Create("ã🦆");
        private static readonly JsonValue MixedString = JsonValue.Create("abc123!@# ");
        private static readonly JsonValue LongString = JsonValue.Create(new string('x', 5000)); // 5000
        private static readonly JsonValue MultiLangString = JsonValue.Create("Hello 你好 مرحبا");
        private static readonly JsonValue JsonString = JsonValue.Create("{\"key\": \"value\"}");
        private static readonly JsonValue HtmlEntityString = JsonValue.Create("&amp; &lt; &gt;");
        private static readonly JsonValue ControlCharString = JsonValue.Create("\t\n\r");
        private static readonly JsonValue SingleCharString = JsonValue.Create("a");
        private static readonly JsonValue LongWordString = JsonValue.Create("Supercalifragilisticexpialidocious");
        private static readonly JsonValue ConcatenatedString = JsonValue.Create("Hello" + "123" + "!@#");
        private static readonly JsonValue WhiteSpaceString = JsonValue.Create("   leading and trailing spaces   ");
        private static readonly JsonValue FilePathString = JsonValue.Create(@"C:\Users\Example\file.txt");
        private static readonly JsonValue LargeNumberString = JsonValue.Create("12345678901234567890");
        private static readonly JsonValue HexadecimalString = JsonValue.Create("0x1A3F");
        private static readonly JsonValue PalindromeString = JsonValue.Create("racecar");
        private static readonly JsonValue SqlInjectionString = JsonValue.Create("SELECT * FROM users WHERE name = 'a'; DROP TABLE users;");
        private static readonly JsonValue RegexString = JsonValue.Create(@"^\d{3}-\d{2}-\d{4}$");
        private static readonly JsonValue DateTimeString = JsonValue.Create("2023-01-01T00:00:00");
        private static readonly JsonValue SpecialCharString = JsonValue.Create("!?@#$%^&*()");
        private static readonly JsonValue SubstringString = JsonValue.Create("Hello world".Substring(0, 5));
        private static readonly JsonValue CaseSensitiveString1 = JsonValue.Create("TestString");
        private static readonly JsonValue CaseSensitiveString2 = JsonValue.Create("teststring");
        private static readonly JsonValue BooleanString = JsonValue.Create("true");
        private static readonly JsonValue FormatSpecifierString = JsonValue.Create("{0:C}");
        private static readonly JsonValue EmojiSequenceString = JsonValue.Create("👨‍👩‍👦");
        private static readonly JsonValue NullCharString = JsonValue.Create("Hello\0World");
        private static readonly JsonValue RepeatingPatternString = JsonValue.Create("abcabcabc");

        [TestMethod]
        public void TestConstructor()
        {
            string s = "hello world";
            JsonValue jstring = JsonValue.Create(s);
            Assert.AreEqual(s, jstring.GetValue<string>());
        }

        [TestMethod]
        public void TestConstructorEmpty()
        {
            string s = "";
            JsonValue jstring = JsonValue.Create(s);
            Assert.AreEqual(s, jstring.GetValue<string>());
        }

        [TestMethod]
        public void TestConstructorSpace()
        {
            string s = "    ";
            JsonValue jstring = JsonValue.Create(s);
            Assert.AreEqual(s, jstring.GetValue<string>());
        }

        [TestMethod]
        public void TestAsNumber()
        {
            Assert.AreEqual(double.NaN, AsicString.AsNumber());
            Assert.AreEqual(double.NaN, EscapeString.AsNumber());
            Assert.AreEqual(double.NaN, BadChar.AsNumber());
            Assert.AreEqual(123, IntegerString.AsNumber());
            Assert.AreEqual(0, EmptyString.AsNumber());
            Assert.AreEqual(double.NaN, SpaceString.AsNumber());
            Assert.AreEqual(123.456, DoubleString.AsNumber());
            Assert.AreEqual(double.NaN, UnicodeString.AsNumber());
            Assert.AreEqual(double.NaN, EmojString.AsNumber());
            Assert.AreEqual(double.NaN, MixedString.AsNumber());
            Assert.AreEqual(double.NaN, LongString.AsNumber());
            Assert.AreEqual(double.NaN, MultiLangString.AsNumber());
            Assert.AreEqual(double.NaN, JsonString.AsNumber());
            Assert.AreEqual(double.NaN, HtmlEntityString.AsNumber());
            Assert.AreEqual(double.NaN, ControlCharString.AsNumber());
            Assert.AreEqual(double.NaN, SingleCharString.AsNumber());
            Assert.AreEqual(double.NaN, LongWordString.AsNumber());
            Assert.AreEqual(double.NaN, ConcatenatedString.AsNumber());
            Assert.AreEqual(double.NaN, WhiteSpaceString.AsNumber());
            Assert.AreEqual(double.NaN, FilePathString.AsNumber());
            Assert.AreEqual(12345678901234567890d, LargeNumberString.AsNumber());
            Assert.AreEqual(double.NaN, HexadecimalString.AsNumber()); // Depending on how hexadecimal strings are handled
            Assert.AreEqual(double.NaN, PalindromeString.AsNumber());
            Assert.AreEqual(double.NaN, SqlInjectionString.AsNumber());
            Assert.AreEqual(double.NaN, RegexString.AsNumber());
            Assert.AreEqual(double.NaN, DateTimeString.AsNumber());
            Assert.AreEqual(double.NaN, SpecialCharString.AsNumber());
            Assert.AreEqual(double.NaN, SubstringString.AsNumber());
            Assert.AreEqual(double.NaN, CaseSensitiveString1.AsNumber());
            Assert.AreEqual(double.NaN, CaseSensitiveString2.AsNumber());
            Assert.AreEqual(double.NaN, BooleanString.AsNumber());
            Assert.AreEqual(double.NaN, FormatSpecifierString.AsNumber());
            Assert.AreEqual(double.NaN, EmojiSequenceString.AsNumber());
            Assert.AreEqual(double.NaN, NullCharString.AsNumber());
            Assert.AreEqual(double.NaN, RepeatingPatternString.AsNumber());
        }

        [TestMethod]
        public void TestValidGetEnum()
        {
            JsonValue validEnum = JsonValue.Create("James");

            Woo woo = validEnum.GetEnum<Woo>();
            Assert.AreEqual(Woo.James, woo);
        }

        [TestMethod]
        public void TestInValidGetEnum()
        {
            JsonValue validEnum = JsonValue.Create("_James");
            Assert.ThrowsExactly<ArgumentException>(() => validEnum.GetEnum<Woo>());
        }

        [TestMethod]
        public void TestMixedString()
        {
            Assert.AreEqual("abc123!@# ", MixedString.GetValue<string>());
        }

        [TestMethod]
        public void TestLongString()
        {
            Assert.AreEqual(new string('x', 5000), LongString.GetValue<string>());
        }

        [TestMethod]
        public void TestMultiLangString()
        {
            Assert.AreEqual("Hello 你好 مرحبا", MultiLangString.GetValue<string>());
        }

        [TestMethod]
        public void TestJsonString()
        {
            Assert.AreEqual("{\"key\": \"value\"}", JsonString.GetValue<string>());
        }

        [TestMethod]
        public void TestHtmlEntityString()
        {
            Assert.AreEqual("&amp; &lt; &gt;", HtmlEntityString.GetValue<string>());
        }

        [TestMethod]
        public void TestControlCharString()
        {
            Assert.AreEqual("\t\n\r", ControlCharString.GetValue<string>());
        }

        [TestMethod]
        public void TestSingleCharString()
        {
            Assert.AreEqual("a", SingleCharString.GetValue<string>());
        }

        [TestMethod]
        public void TestLongWordString()
        {
            Assert.AreEqual("Supercalifragilisticexpialidocious", LongWordString.GetValue<string>());
        }

        [TestMethod]
        public void TestConcatenatedString()
        {
            Assert.AreEqual("Hello123!@#", ConcatenatedString.GetValue<string>());
        }

        [TestMethod]
        public void TestWhiteSpaceString()
        {
            Assert.AreEqual("   leading and trailing spaces   ", WhiteSpaceString.GetValue<string>());
        }

        [TestMethod]
        public void TestFilePathString()
        {
            Assert.AreEqual(@"C:\Users\Example\file.txt", FilePathString.GetValue<string>());
        }

        [TestMethod]
        public void TestLargeNumberString()
        {
            Assert.AreEqual("12345678901234567890", LargeNumberString.GetValue<string>());
        }

        [TestMethod]
        public void TestHexadecimalString()
        {
            Assert.AreEqual("0x1A3F", HexadecimalString.GetValue<string>());
        }

        [TestMethod]
        public void TestPalindromeString()
        {
            Assert.AreEqual("racecar", PalindromeString.GetValue<string>());
        }

        [TestMethod]
        public void TestSqlInjectionString()
        {
            Assert.AreEqual("SELECT * FROM users WHERE name = 'a'; DROP TABLE users;", SqlInjectionString.GetValue<string>());
        }

        [TestMethod]
        public void TestRegexString()
        {
            Assert.AreEqual(@"^\d{3}-\d{2}-\d{4}$", RegexString.GetValue<string>());
        }

        [TestMethod]
        public void TestDateTimeString()
        {
            Assert.AreEqual("2023-01-01T00:00:00", DateTimeString.GetValue<string>());
        }

        [TestMethod]
        public void TestSpecialCharString()
        {
            Assert.AreEqual("!?@#$%^&*()", SpecialCharString.GetValue<string>());
        }

        [TestMethod]
        public void TestSubstringString()
        {
            Assert.AreEqual("Hello", SubstringString.GetValue<string>());
        }

        [TestMethod]
        public void TestCaseSensitiveStrings()
        {
            Assert.AreNotEqual(CaseSensitiveString1.GetValue<string>(), CaseSensitiveString2.GetValue<string>());
        }

        [TestMethod]
        public void TestBooleanString()
        {
            Assert.AreEqual("true", BooleanString.GetValue<string>());
        }

        [TestMethod]
        public void TestFormatSpecifierString()
        {
            Assert.AreEqual("{0:C}", FormatSpecifierString.GetValue<string>());
        }

        [TestMethod]
        public void TestEmojiSequenceString()
        {
            Assert.AreEqual("👨‍👩‍👦", EmojiSequenceString.GetValue<string>());
        }

        [TestMethod]
        public void TestNullCharString()
        {
            Assert.AreEqual("Hello\0World", NullCharString.GetValue<string>());
        }

        [TestMethod]
        public void TestRepeatingPatternString()
        {
            Assert.AreEqual("abcabcabc", RepeatingPatternString.GetValue<string>());
        }

        [TestMethod]
        public void TestEqual()
        {
            var str = "hello world";
            var str2 = "hello world2";
            var jString = JsonValue.Create(str);
            var jString2 = JsonValue.Create(str2);
            Assert.IsFalse(jString == null);

            Assert.AreEqual(str, jString.GetValue<string>());
            Assert.IsTrue(jString.Equals(str));
            Assert.IsFalse(jString.Equals(jString2));
            Assert.IsFalse(jString.Equals(null));
            Assert.IsFalse(jString.Equals(123));
            var reference = jString;
            Assert.IsTrue(jString.Equals(reference));
        }

        [TestMethod]
        public void TestWrite()
        {
            var jString = JsonValue.Create("hello world");
            using (var stream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(stream))
            {
                jString.WriteTo(writer);
                writer.Flush();
                var json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.AreEqual("\"hello world\"", json);
            }
        }

        [TestMethod]
        public void TestClone()
        {
            var jString = JsonValue.Create("hello world");
            var clone = jString.DeepClone();
            Assert.AreEqual(jString, clone);
            Assert.AreSame(jString, clone); // Cloning should return the same instance for immutable objects
        }

        [TestMethod]
        public void TestEqualityWithDifferentTypes()
        {
            var jString = JsonValue.Create("hello world");
            Assert.IsFalse(jString.Equals(123));
            Assert.IsFalse(jString.Equals(new object()));
            Assert.IsFalse(jString.Equals(JsonValue.Create(false)));
        }

        [TestMethod]
        public void TestImplicitOperators()
        {
            JsonValue fromEnum = JsonValue.Create(EnumExample.Value.ToString());
            Assert.AreEqual("Value", fromEnum.GetValue<string>());

            JsonValue fromString = JsonValue.Create("test string");
            Assert.AreEqual("test string", fromString.GetValue<string>());
        }

        [TestMethod]
        public void TestBoundaryAndSpecialCases()
        {
            JsonValue largeString = JsonValue.Create(new string('a', ushort.MaxValue));
            Assert.AreEqual(ushort.MaxValue, largeString.GetValue<string>().Length);

            JsonValue specialUnicode = JsonValue.Create("\uD83D\uDE00"); // 😀 emoji
            Assert.AreEqual("\uD83D\uDE00", specialUnicode.GetValue<string>());

            JsonValue complexJson = JsonValue.Create("{\"nested\":{\"key\":\"value\"}}");
            Assert.AreEqual("{\"nested\":{\"key\":\"value\"}}", complexJson.GetValue<string>());
        }

        [TestMethod]
        public void TestExceptionHandling()
        {
            JsonValue invalidEnum = JsonValue.Create("invalid_value");
            Assert.ThrowsExactly<ArgumentException>(() => _ = invalidEnum.GetEnum<Woo>());
        }
    }
    public enum EnumExample
    {
        Value
    }
}
