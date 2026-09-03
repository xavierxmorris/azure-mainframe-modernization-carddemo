using System.Globalization;

namespace CardDemo.Modern.Services;

internal static class FixedWidth
{
    // CardDemo's ASCII export encodes signed numeric fields as zoned decimals with the
    // sign overpunched onto the LAST digit. Leading-sign overpunch, explicit '+'/'-'
    // characters, and embedded whitespace are intentionally rejected rather than
    // guessed at, so that an unexpected layout fails loudly instead of silently
    // producing a wrong monetary value.
    private const string PositiveOverpunch = "{ABCDEFGHI";
    private const string NegativeOverpunch = "}JKLMNOPQR";

    public static string Text(string line, int start, int length)
    {
        EnsureRange(line, start, length);
        return line.Substring(start, length).Trim();
    }

    public static decimal SignedDecimal(string line, int start, int length, int scale)
    {
        var raw = Text(line, start, length);
        if (raw.Length == 0)
        {
            throw new FormatException("A signed COBOL decimal field cannot be blank.");
        }

        var last = raw[^1];
        var sign = 1;
        var digit = last;
        var positiveIndex = PositiveOverpunch.IndexOf(last);
        var negativeIndex = NegativeOverpunch.IndexOf(last);

        if (positiveIndex >= 0)
        {
            digit = (char)('0' + positiveIndex);
        }
        else if (negativeIndex >= 0)
        {
            sign = -1;
            digit = (char)('0' + negativeIndex);
        }

        var normalized = raw[..^1] + digit;
        if (!long.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("The field is not a valid signed COBOL decimal.");
        }

        return sign * value / Pow10(scale);
    }

    public static int Integer(string line, int start, int length)
    {
        var raw = Text(line, start, length);
        if (raw.Length == 0
            || !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("The field is not a valid unsigned COBOL integer.");
        }

        return value;
    }

    public static DateOnly? Date(string line, int start, int length)
    {
        var raw = Text(line, start, length);
        if (raw.Length == 0)
        {
            return null;
        }

        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var value))
        {
            throw new FormatException("The field is not a valid yyyy-MM-dd date.");
        }

        return value;
    }

    public static DateTime? Timestamp(string line, int start, int length)
    {
        var raw = Text(line, start, length);
        if (raw.Length == 0)
        {
            return null;
        }

        if (!DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss.ffffff",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            throw new FormatException("The field is not a valid CardDemo timestamp.");
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    public static void RequireLength(string line, int expected, string source)
    {
        if (line.Length != expected)
        {
            throw new InvalidDataException(
                $"{source} record has length {line.Length}; expected exactly {expected}.");
        }
    }

    private static decimal Pow10(int scale) => scale switch
    {
        0 => 1m,
        1 => 10m,
        2 => 100m,
        3 => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(scale))
    };

    private static void EnsureRange(string line, int start, int length)
    {
        if (start < 0 || length < 0 || start + length > line.Length)
        {
            throw new InvalidDataException(
                $"Fixed-width range {start}..{start + length} exceeds record length {line.Length}.");
        }
    }
}
