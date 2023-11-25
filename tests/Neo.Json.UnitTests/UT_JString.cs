namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JString
    {
        private static readonly JString AsicString = "hello world";
        private static readonly JString EscapeString = "\n\t\'\"";
        private static readonly JString BadChar = ((char)0xff).ToString();
        private static readonly JString IntegerString = "123";
        private static readonly JString EmptyString = "";
        private static readonly JString SpaceString = "    ";
        private static readonly JString DoubleString = "123.456";
        private static readonly JString UnicodeString = "\ud83d\ude03\ud83d\ude01";
        private static readonly JString EmojString = "ã🦆";
        private static readonly JString MixedString = "abc123!@# ";
        private static readonly JString LongString = new String('x', 5000); // 5000
        private static readonly JString MultiLangString = "Hello 你好 مرحبا";
        private static readonly JString JsonString = "{\"key\": \"value\"}";
        private static readonly JString HtmlEntityString = "&amp; &lt; &gt;";
        private static readonly JString ControlCharString = "\t\n\r";
        private static readonly JString SingleCharString = "a";
        private static readonly JString LongWordString = "Supercalifragilisticexpialidocious";
        private static readonly JString ConcatenatedString = new JString("Hello" + "123" + "!@#");
        private static readonly JString WhiteSpaceString = new JString("   leading and trailing spaces   ");
        private static readonly JString FilePathString = new JString(@"C:\Users\Example\file.txt");
        private static readonly JString LargeNumberString = new JString("12345678901234567890");
        private static readonly JString HexadecimalString = new JString("0x1A3F");
        private static readonly JString PalindromeString = new JString("racecar");
        private static readonly JString SqlInjectionString = new JString("SELECT * FROM users WHERE name = 'a'; DROP TABLE users;");
        private static readonly JString RegexString = new JString(@"^\d{3}-\d{2}-\d{4}$");
        private static readonly JString DateTimeString = new JString("2023-01-01T00:00:00");
        private static readonly JString SpecialCharString = new JString("!?@#$%^&*()");
        private static readonly JString SubstringString = new JString("Hello world".Substring(0, 5));
        private static readonly JString CaseSensitiveString1 = new JString("TestString");
        private static readonly JString CaseSensitiveString2 = new JString("teststring");
        private static readonly JString BooleanString = new JString("true");
        private static readonly JString FormatSpecifierString = new JString("{0:C}");
        private static readonly JString EmojiSequenceString = new JString("👨‍👩‍👦");
        private static readonly JString NullCharString = new JString("Hello\0World");
        private static readonly JString RepeatingPatternString = new JString("abcabcabc");

        [TestMethod]
        public void TestConstructor()
        {
            string s = "hello world";
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorNull()
        {
            string s = null;
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null!));
        }

        [TestMethod]
        public void TestConstructorEmpty()
        {
            string s = "";
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
        }

        [TestMethod]
        public void TestConstructorSpace()
        {
            string s = "    ";
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null));
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            Assert.AreEqual(true, AsicString.AsBoolean());
            Assert.AreEqual(true, EscapeString.AsBoolean());
            Assert.AreEqual(true, BadChar.AsBoolean());
            Assert.AreEqual(true, IntegerString.AsBoolean());
            Assert.AreEqual(false, EmptyString.AsBoolean());
            Assert.AreEqual(true, SpaceString.AsBoolean());
            Assert.AreEqual(true, DoubleString.AsBoolean());
            Assert.AreEqual(true, UnicodeString.AsBoolean());
            Assert.AreEqual(true, EmojString.AsBoolean());
            Assert.AreEqual(true, MixedString.AsBoolean());
            Assert.AreEqual(true, LongString.AsBoolean());
            Assert.AreEqual(true, MultiLangString.AsBoolean());
            Assert.AreEqual(true, JsonString.AsBoolean());
            Assert.AreEqual(true, HtmlEntityString.AsBoolean());
            Assert.AreEqual(true, ControlCharString.AsBoolean());
            Assert.AreEqual(true, SingleCharString.AsBoolean());
            Assert.AreEqual(true, LongWordString.AsBoolean());
            Assert.AreEqual(true, ConcatenatedString.AsBoolean());
            Assert.AreEqual(true, WhiteSpaceString.AsBoolean());
            Assert.AreEqual(true, FilePathString.AsBoolean());
            Assert.AreEqual(true, LargeNumberString.AsBoolean());
            Assert.AreEqual(true, HexadecimalString.AsBoolean());
            Assert.AreEqual(true, PalindromeString.AsBoolean());
            Assert.AreEqual(true, SqlInjectionString.AsBoolean());
            Assert.AreEqual(true, RegexString.AsBoolean());
            Assert.AreEqual(true, DateTimeString.AsBoolean());
            Assert.AreEqual(true, SpecialCharString.AsBoolean());
            Assert.AreEqual(true, SubstringString.AsBoolean());
            Assert.AreEqual(true, CaseSensitiveString1.AsBoolean());
            Assert.AreEqual(true, CaseSensitiveString2.AsBoolean());
            Assert.AreEqual(true, BooleanString.AsBoolean());
            Assert.AreEqual(true, FormatSpecifierString.AsBoolean());
            Assert.AreEqual(true, EmojiSequenceString.AsBoolean());
            Assert.AreEqual(true, NullCharString.AsBoolean());
            Assert.AreEqual(true, RepeatingPatternString.AsBoolean());
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
            JString validEnum = "James";

            Woo woo = validEnum.GetEnum<Woo>();
            Assert.AreEqual(Woo.James, woo);

            validEnum = "";
            woo = validEnum.AsEnum(Woo.Jerry, false);
            Assert.AreEqual(Woo.Jerry, woo);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInValidGetEnum()
        {
            JString validEnum = "_James";
            Woo woo = validEnum.GetEnum<Woo>();
        }

        [TestMethod]
        public void TestMixedString()
        {
            Assert.AreEqual("abc123!@# ", MixedString.Value);
        }

        [TestMethod]
        public void TestLongString()
        {
            Assert.AreEqual(new String('x', 5000), LongString.Value);
        }

        [TestMethod]
        public void TestMultiLangString()
        {
            Assert.AreEqual("Hello 你好 مرحبا", MultiLangString.Value);
        }

        [TestMethod]
        public void TestJsonString()
        {
            Assert.AreEqual("{\"key\": \"value\"}", JsonString.Value);
        }

        [TestMethod]
        public void TestHtmlEntityString()
        {
            Assert.AreEqual("&amp; &lt; &gt;", HtmlEntityString.Value);
        }

        [TestMethod]
        public void TestControlCharString()
        {
            Assert.AreEqual("\t\n\r", ControlCharString.Value);
        }

        [TestMethod]
        public void TestSingleCharString()
        {
            Assert.AreEqual("a", SingleCharString.Value);
        }

        [TestMethod]
        public void TestLongWordString()
        {
            Assert.AreEqual("Supercalifragilisticexpialidocious", LongWordString.Value);
        }

        [TestMethod]
        public void TestConcatenatedString()
        {
            Assert.AreEqual("Hello123!@#", ConcatenatedString.Value);
        }

        [TestMethod]
        public void TestWhiteSpaceString()
        {
            Assert.AreEqual("   leading and trailing spaces   ", WhiteSpaceString.Value);
        }

        [TestMethod]
        public void TestFilePathString()
        {
            Assert.AreEqual(@"C:\Users\Example\file.txt", FilePathString.Value);
        }

        [TestMethod]
        public void TestLargeNumberString()
        {
            Assert.AreEqual("12345678901234567890", LargeNumberString.Value);
        }

        [TestMethod]
        public void TestHexadecimalString()
        {
            Assert.AreEqual("0x1A3F", HexadecimalString.Value);
        }

        [TestMethod]
        public void TestPalindromeString()
        {
            Assert.AreEqual("racecar", PalindromeString.Value);
        }

        [TestMethod]
        public void TestSqlInjectionString()
        {
            Assert.AreEqual("SELECT * FROM users WHERE name = 'a'; DROP TABLE users;", SqlInjectionString.Value);
        }

        [TestMethod]
        public void TestRegexString()
        {
            Assert.AreEqual(@"^\d{3}-\d{2}-\d{4}$", RegexString.Value);
        }

        [TestMethod]
        public void TestDateTimeString()
        {
            Assert.AreEqual("2023-01-01T00:00:00", DateTimeString.Value);
        }

        [TestMethod]
        public void TestSpecialCharString()
        {
            Assert.AreEqual("!?@#$%^&*()", SpecialCharString.Value);
        }

        [TestMethod]
        public void TestSubstringString()
        {
            Assert.AreEqual("Hello", SubstringString.Value);
        }

        [TestMethod]
        public void TestCaseSensitiveStrings()
        {
            Assert.AreNotEqual(CaseSensitiveString1.Value, CaseSensitiveString2.Value);
        }

        [TestMethod]
        public void TestBooleanString()
        {
            Assert.AreEqual("true", BooleanString.Value);
        }

        [TestMethod]
        public void TestFormatSpecifierString()
        {
            Assert.AreEqual("{0:C}", FormatSpecifierString.Value);
        }

        [TestMethod]
        public void TestEmojiSequenceString()
        {
            Assert.AreEqual("👨‍👩‍👦", EmojiSequenceString.Value);
        }

        [TestMethod]
        public void TestNullCharString()
        {
            Assert.AreEqual("Hello\0World", NullCharString.Value);
        }

        [TestMethod]
        public void TestRepeatingPatternString()
        {
            Assert.AreEqual("abcabcabc", RepeatingPatternString.Value);
        }

    }
}
