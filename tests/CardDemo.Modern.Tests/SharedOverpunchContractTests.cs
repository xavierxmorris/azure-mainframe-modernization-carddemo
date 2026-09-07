using System.Globalization;
using CardDemo.Modern.Services;

namespace CardDemo.Modern.Tests;

public sealed class SharedOverpunchContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Fixtures", "signed-overpunch.txt");
        foreach (var line in File.ReadLines(file))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            var columns = line.Split('|');
            if (columns.Length != 3)
            {
                throw new InvalidDataException("Contract rows must have exactly three columns.");
            }
            yield return [columns[0], int.Parse(columns[1], CultureInfo.InvariantCulture), columns[2]];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SignedDecimal_MatchesSharedJavaContract(string field, int scale, string expected)
    {
        if (expected == "ERROR")
        {
            var error = Record.Exception(() => FixedWidth.SignedDecimal(field, 0, field.Length, scale));
            Assert.True(error is FormatException or ArgumentOutOfRangeException,
                "Malformed fields must fail with a numeric format or scale error.");
            return;
        }

        var actual = FixedWidth.SignedDecimal(field, 0, field.Length, scale);
        Assert.Equal(expected, actual.ToString($"F{scale}", CultureInfo.InvariantCulture));
    }
}
