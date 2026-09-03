using CardDemo.Modern.Services;

namespace CardDemo.Modern.Tests;

public sealed class FixedWidthTests
{
    [Theory]
    [InlineData("00000001940{", 194.00)]
    [InlineData("0000009190}", -919.00)]
    [InlineData("0000000678H", 67.88)]
    [InlineData("0000000000{", 0.00)]
    public void SignedDecimal_DecodesCobolOverpunch(string value, decimal expected)
    {
        Assert.Equal(expected, FixedWidth.SignedDecimal(value, 0, value.Length, 2));
    }

    [Fact]
    public void Text_RejectsRangesOutsideRecord()
    {
        var exception = Assert.Throws<InvalidDataException>(() => FixedWidth.Text("ABC", 2, 2));

        Assert.Contains("record length 3", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SignedDecimal_RejectsBlankAndInvalidOverpunch()
    {
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("           ", 0, 11, 2));
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("0000000123Z", 0, 11, 2));
    }

    [Fact]
    public void Date_AllowsBlankButRejectsMalformedValues()
    {
        Assert.Null(FixedWidth.Date("          ", 0, 10));
        Assert.Throws<FormatException>(() => FixedWidth.Date("2025-99-99", 0, 10));
    }

    [Fact]
    public void Timestamp_PreservesUnspecifiedSourceTime()
    {
        var timestamp = FixedWidth.Timestamp("2022-06-10 19:27:53.000000", 0, 26);

        Assert.NotNull(timestamp);
        Assert.Equal(DateTimeKind.Unspecified, timestamp.Value.Kind);
    }

    [Fact]
    public void SignedDecimal_RejectsLeadingSignConventions()
    {
        // The parser contract is trailing overpunch only. Leading overpunch and
        // explicit sign characters must fail loudly rather than be reinterpreted.
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("}0000009190", 0, 11, 2));
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("-0000009190", 0, 11, 2));
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("+0000009190", 0, 11, 2));
    }

    [Fact]
    public void SignedDecimal_RejectsEmbeddedWhitespace()
    {
        Assert.Throws<FormatException>(() => FixedWidth.SignedDecimal("00000 1940{", 0, 11, 2));
    }

    [Fact]
    public void RequireLength_RejectsShortAndLongRecords()
    {
        Assert.Throws<InvalidDataException>(() => FixedWidth.RequireLength("123", 4, "test"));
        Assert.Throws<InvalidDataException>(() => FixedWidth.RequireLength("12345", 4, "test"));
    }
}
