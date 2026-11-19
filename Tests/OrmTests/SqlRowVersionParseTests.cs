using System;
using System.Globalization;
using Xunit;

namespace AltaSoft.Storm.Tests;

public class SqlRowVersionParseTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(0x1234567890123456UL)] // ToString() is all digits => must still parse as hex
    [InlineData(0x00000000000007D1UL)]
    [InlineData(1000000000000000UL)] // decimal form has exactly 16 digits (Convert.ToString used to emit decimal)
    [InlineData(ulong.MaxValue)]
    public void Parse_RoundTripsToString(ulong value)
    {
        SqlRowVersion rv = value;
        Assert.Equal(rv, SqlRowVersion.Parse(rv.ToString()));
        Assert.Equal(rv, SqlRowVersion.Parse("0x" + rv.ToString()));
        // Convert.ToString(object) uses IConvertible.ToString and must round-trip too (the cast avoids the implicit ulong conversion)
        Assert.Equal(rv, SqlRowVersion.Parse(Convert.ToString((object)rv, CultureInfo.InvariantCulture)!));
    }

    [Theory]
    [InlineData("0x1F", 0x1FUL)]
    [InlineData("0x10", 0x10UL)] // was parsed as decimal 10
    [InlineData("0X7d1", 0x7D1UL)]
    [InlineData("  0x00000000000007D1 ", 0x7D1UL)]
    [InlineData("2001", 2001UL)]
    [InlineData("123456789012345", 123456789012345UL)] // 15 digits => decimal
    [InlineData("12345678901234567", 12345678901234567UL)] // 17 digits => decimal
    [InlineData("1234567890123456", 0x1234567890123456UL)] // exactly 16 chars => hex (documented ToString format)
    public void Parse_FollowsDocumentedRules(string input, ulong expected)
    {
        Assert.Equal((SqlRowVersion)expected, SqlRowVersion.Parse(input, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0x")]
    [InlineData("0x00000000000000001")] // 17 hex digits
    [InlineData("0xZZ")]
    [InlineData("12AB")] // not 16 chars and not decimal
    public void Parse_InvalidInput_Throws(string input)
    {
        Assert.ThrowsAny<FormatException>(() => SqlRowVersion.Parse(input));
        Assert.False(SqlRowVersion.TryParse(input, null, out _));
    }

    [Fact]
    public void Utf8Format_DefaultsToHex()
    {
        SqlRowVersion rv = 0x7D1UL;
        Span<byte> buffer = stackalloc byte[32];
        Assert.True(((IUtf8SpanFormattable)rv).TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal("00000000000007D1", System.Text.Encoding.UTF8.GetString(buffer[..written]));
    }
}
