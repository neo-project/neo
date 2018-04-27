using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UInt256
    {
        [TestMethod]
        public void Is_zero_by_default()
        {
            var actual = new UInt256();
            (actual == UInt256.Zero).Should().BeTrue();
        }

        [TestMethod]
        public void Throw_when_intialized_with_buffer_non_equal_to_size()
        {
            Action a = () => new UInt256(new byte[]
                { 157, 179, 60, 8, 66, 122, 255, 105, 126, 49, 180, 74, 212, 41, 126, 177, 14, 255, 59, 82, 218, 113, 248, 145, 98, 5, 128, 140, 42, 70, 32, 69, 0 });

            a.ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        public void Can_be_equal_to_another_number_of_same_type()
        {
            var a = new UInt256(new byte[]
                { 157, 179, 60, 8, 66, 122, 255, 105, 126, 49, 180, 74, 212, 41, 126, 177, 14, 255, 59, 82, 218, 113, 248, 145, 98, 5, 128, 140, 42, 70, 32, 69 });
            var b = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            (a == b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_be_not_equal_to_another_number_of_same_type()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            var b = UInt256.Zero;
            (a == b).Should().BeFalse();
        }

        [TestMethod]
        public void Can_be_not_equal_to_null_of_same_type()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            var b = default(UInt256);
            a.Equals(b).Should().BeFalse();
        }

        [TestMethod]
        public void Can_be_not_equal_to_null_of_object_type()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");          
            var b = default(object);
            a.Equals(b).Should().BeFalse();
        }

        [TestMethod]
        public void Can_be_equal_to_another_number_of_object_type()
        {
            var a = new UInt256(new byte[]
                { 157, 179, 60, 8, 66, 122, 255, 105, 126, 49, 180, 74, 212, 41, 126, 177, 14, 255, 59, 82, 218, 113, 248, 145, 98, 5, 128, 140, 42, 70, 32, 69 });
            var b = (object)UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            a.Equals(b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_be_not_equal_to_another_number_of_object_type()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            var b = (object)1;
            a.Equals(b).Should().BeFalse();
        }

        [TestMethod]
        public void Can_be_greater_than_another_number()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39e");
            var b = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            (a > b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_be_greater_than_another_number_or_equal()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39e");
            var b = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            (a >= b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_be_less_than_another_number()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39c");
            var b = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            (a < b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_be_less_than_another_number_or_equal()
        {
            var a = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39c");
            var b = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            (a <= b).Should().BeTrue();
        }

        [TestMethod]
        public void Can_parse_and_stringify()
        {
            var actual = UInt256.Parse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            actual.ToString().Should().Be("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
        }

        [TestMethod]
        public void Can_parse_valid_string_safely()
        {
            var actual = UInt256.TryParse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d", out UInt256 a);
            actual.Should().BeTrue();
            a.Should().NotBe(UInt256.Zero);
        }

        [TestMethod]
        public void Can_parse_invalid_string_safely()
        {
            var actual =  UInt256.TryParse("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39dx0", out UInt256 a);
            actual.Should().BeFalse();
            a.Should().Be(UInt256.Zero);
        }
    }
}