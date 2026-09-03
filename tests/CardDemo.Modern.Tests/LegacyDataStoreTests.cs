using CardDemo.Modern.Services;

namespace CardDemo.Modern.Tests;

public sealed class LegacyDataStoreTests
{
    private static readonly string[] RequiredFiles =
    [
        "acctdata.txt",
        "carddata.txt",
        "cardxref.txt",
        "custdata.txt",
        "dailytran.txt"
    ];

    private readonly LegacyDataStore _store = new(GetDataDirectory());

    [Fact]
    public void Summary_ReflectsCheckedInLegacyDatasets()
    {
        Assert.Equal(50, _store.Summary.AccountCount);
        Assert.Equal(50, _store.Summary.CardCount);
        Assert.Equal(50, _store.Summary.CustomerCount);
        Assert.Equal(300, _store.Summary.TransactionCount);
    }

    [Fact]
    public void SearchAccounts_AssemblesValidatedCrossFileView()
    {
        var result = Assert.Single(_store.SearchAccounts("00000000001"));

        Assert.Equal("00000000001", result.Id);
        Assert.StartsWith("Sample customer ", result.CardholderLabel, StringComparison.Ordinal);
        Assert.True(result.CardCount > 0);
        Assert.True(result.TransactionCount > 0);
        Assert.Equal(1020m, result.CashCreditLimit);
    }

    [Fact]
    public void GetAccount_ReturnsMaskedPseudonymizedViews()
    {
        var result = Assert.IsType<CardDemo.Modern.Models.AccountDetail>(
            _store.GetAccount("00000000001"));

        Assert.NotEmpty(result.Cards);
        Assert.All(result.Cards, card =>
        {
            Assert.StartsWith("•••• •••• •••• ", card.MaskedNumber, StringComparison.Ordinal);
            Assert.Equal(19, card.MaskedNumber.Length);
            Assert.StartsWith("Sample customer ", card.CardholderLabel, StringComparison.Ordinal);
        });
        Assert.All(result.Customers, customer =>
            Assert.StartsWith("Sample customer ", customer.DisplayLabel, StringComparison.Ordinal));
        Assert.Contains(result.Transactions, transaction => transaction.Amount == -70.77m);
        Assert.All(
            result.Transactions.Where(transaction => transaction.OriginalTimestamp is not null),
            transaction => Assert.Equal(DateTimeKind.Unspecified, transaction.OriginalTimestamp!.Value.Kind));
    }

    [Fact]
    public void GetAccount_ReturnsNullForUnknownId()
    {
        Assert.Null(_store.GetAccount("99999999999"));
    }

    [Fact]
    public void Constructor_RejectsMismatchedCardCrossReference()
    {
        WithDataCopy(directory =>
        {
            var path = Path.Combine(directory, "cardxref.txt");
            var records = File.ReadAllLines(path);
            var replacementAccount = records[0].Substring(25, 11) == "00000000001"
                ? "00000000002"
                : "00000000001";
            records[0] = records[0][..25] + replacementAccount;
            File.WriteAllLines(path, records);

            Assert.Throws<InvalidDataException>(() => new LegacyDataStore(directory));
        });
    }

    [Fact]
    public void Constructor_RejectsMalformedNonblankDate()
    {
        WithDataCopy(directory =>
        {
            var path = Path.Combine(directory, "carddata.txt");
            var records = File.ReadAllLines(path);
            records[0] = records[0][..80] + "2025-99-99" + records[0][90..];
            File.WriteAllLines(path, records);

            Assert.Throws<FormatException>(() => new LegacyDataStore(directory));
        });
    }

    [Fact]
    public void Constructor_EnforcesDocumentedAsciiCrossReferenceLength()
    {
        WithDataCopy(directory =>
        {
            var path = Path.Combine(directory, "cardxref.txt");
            var records = File.ReadAllLines(path);
            records[0] += new string(' ', 14);
            File.WriteAllLines(path, records);

            Assert.Throws<InvalidDataException>(() => new LegacyDataStore(directory));
        });
    }

    private static void WithDataCopy(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"carddemo-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var fileName in RequiredFiles)
            {
                File.Copy(Path.Combine(GetDataDirectory(), fileName), Path.Combine(directory, fileName));
            }

            action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string GetDataDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "app", "data", "ASCII"));
}
