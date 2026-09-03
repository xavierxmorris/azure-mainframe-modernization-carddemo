using CardDemo.Modern.Models;

namespace CardDemo.Modern.Services;

public sealed class LegacyDataStore
{
    private readonly IReadOnlyList<LegacyAccount> _accounts;
    private readonly IReadOnlyList<LegacyCard> _cards;
    private readonly IReadOnlyList<LegacyCustomer> _customers;
    private readonly IReadOnlyList<LegacyTransaction> _transactions;
    private readonly IReadOnlyList<LegacyCardCrossReference> _crossReferences;

    public LegacyDataStore()
        : this(Path.Combine(AppContext.BaseDirectory, "Data"))
    {
    }

    internal LegacyDataStore(string dataDirectory)
    {
        _accounts = ReadLines(dataDirectory, "acctdata.txt").Select(ParseAccount).ToArray();
        _cards = ReadLines(dataDirectory, "carddata.txt").Select(ParseCard).ToArray();
        _customers = ReadLines(dataDirectory, "custdata.txt").Select(ParseCustomer).ToArray();
        _transactions = ReadLines(dataDirectory, "dailytran.txt").Select(ParseTransaction).ToArray();
        _crossReferences = ReadLines(dataDirectory, "cardxref.txt").Select(ParseCrossReference).ToArray();

        ValidateData();

        Summary = new DashboardSummary(
            _accounts.Count,
            _accounts.Count(account => account.IsActive),
            _cards.Count,
            _customers.Count,
            _transactions.Count);
    }

    public DashboardSummary Summary { get; }

    /// <summary>
    /// Account identifiers are <c>PIC 9(11)</c> in <c>CVACT01Y</c>. Rejecting anything
    /// else keeps longer digit strings, such as a pasted card number, out of responses
    /// and request logs.
    /// </summary>
    public static bool IsValidAccountId(string? accountId) =>
        accountId is { Length: 11 } && accountId.All(char.IsAsciiDigit);

    /// <summary>
    /// Search terms are bounded to the width of an account identifier so that a pasted
    /// card number can never be reflected back to the caller.
    /// </summary>
    public static bool IsValidSearchTerm(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalized = query.Trim();
        return normalized.Length <= 11 && normalized.All(char.IsAsciiDigit);
    }

    public IReadOnlyList<AccountListItem> SearchAccounts(string? query = null)
    {
        var normalized = query?.Trim();
        return _accounts
            .Select(ToListItem)
            .Where(account => string.IsNullOrWhiteSpace(normalized)
                || account.Id.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(account => account.CurrentBalance)
            .ToArray();
    }

    public AccountDetail? GetAccount(string accountId)
    {
        var account = _accounts.FirstOrDefault(item =>
            item.Id.Equals(accountId, StringComparison.Ordinal));
        if (account is null)
        {
            return null;
        }

        var cards = _cards.Where(card => card.AccountId == account.Id).ToArray();
        var cardNumbers = cards.Select(card => card.Number).ToHashSet(StringComparer.Ordinal);
        var references = _crossReferences
            .Where(reference =>
                reference.AccountId == account.Id
                && cardNumbers.Contains(reference.CardNumber))
            .ToArray();
        var customerIds = references
            .Select(reference => reference.CustomerId)
            .ToHashSet(StringComparer.Ordinal);

        return new AccountDetail(
            ToListItem(account),
            cards.Select(card => new CardView(
                MaskCard(card.Number),
                GetCardholderLabel(card.Number),
                card.IsActive)).ToArray(),
            _customers
                .Where(customer => customerIds.Contains(customer.Id))
                .Select(ToCustomerView)
                .ToArray(),
            _transactions
                .Where(transaction => cardNumbers.Contains(transaction.CardNumber))
                .OrderByDescending(transaction => transaction.OriginalTimestamp)
                .Select(transaction => new TransactionView(
                    transaction.Id,
                    transaction.TypeCode,
                    transaction.CategoryCode,
                    transaction.Source,
                    transaction.Description,
                    transaction.Amount,
                    transaction.MerchantName,
                    transaction.MerchantCity,
                    transaction.OriginalTimestamp))
                .ToArray());
    }

    private AccountListItem ToListItem(LegacyAccount account)
    {
        var cards = _cards.Where(card => card.AccountId == account.Id).ToArray();
        var cardNumbers = cards.Select(card => card.Number).ToHashSet(StringComparer.Ordinal);
        var customerIds = _crossReferences
            .Where(reference =>
                reference.AccountId == account.Id
                && cardNumbers.Contains(reference.CardNumber))
            .Select(reference => reference.CustomerId)
            .ToHashSet(StringComparer.Ordinal);
        var primaryCustomer = _customers.FirstOrDefault(customer =>
            customerIds.Contains(customer.Id) && customer.IsPrimaryCardHolder)
            ?? _customers.FirstOrDefault(customer => customerIds.Contains(customer.Id));

        return new AccountListItem(
            account.Id,
            account.IsActive,
            account.CurrentBalance,
            account.CreditLimit,
            account.CashCreditLimit,
            cards.Length,
            _transactions.Count(transaction => cardNumbers.Contains(transaction.CardNumber)),
            primaryCustomer is null ? "Unlinked record" : Pseudonym(primaryCustomer));
    }

    private static CustomerView ToCustomerView(LegacyCustomer customer) => new(
        Pseudonym(customer),
        string.Join(", ", new[] { customer.State, customer.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value))),
        customer.IsPrimaryCardHolder);

    private string GetCardholderLabel(string cardNumber)
    {
        var customerId = _crossReferences.Single(reference =>
            reference.CardNumber.Equals(cardNumber, StringComparison.Ordinal)).CustomerId;
        var customer = _customers.Single(item =>
            item.Id.Equals(customerId, StringComparison.Ordinal));
        return Pseudonym(customer);
    }

    private void ValidateData()
    {
        ValidateUnique(_accounts, account => account.Id, "Account data");
        ValidateUnique(_cards, card => card.Number, "Card data");
        ValidateUnique(_customers, customer => customer.Id, "Customer data");
        ValidateUnique(_transactions, transaction => transaction.Id, "Transaction data");
        ValidateUnique(_crossReferences, reference => reference.CardNumber, "Card cross-reference data");

        var accountIds = _accounts.Select(account => account.Id).ToHashSet(StringComparer.Ordinal);
        var cardsByNumber = _cards.ToDictionary(card => card.Number, StringComparer.Ordinal);
        var customerIds = _customers.Select(customer => customer.Id).ToHashSet(StringComparer.Ordinal);

        for (var index = 0; index < _cards.Count; index++)
        {
            if (!accountIds.Contains(_cards[index].AccountId))
            {
                throw new InvalidDataException($"Card record {index + 1} references an unknown account.");
            }
        }

        // GetCardholderLabel resolves a card to exactly one cross-reference, so the
        // one-to-one relationship must hold before any request is served.
        var referencedCardNumbers = _crossReferences
            .Select(reference => reference.CardNumber)
            .ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < _cards.Count; index++)
        {
            if (!referencedCardNumbers.Contains(_cards[index].Number))
            {
                throw new InvalidDataException(
                    $"Card record {index + 1} has no card cross-reference record.");
            }
        }

        for (var index = 0; index < _crossReferences.Count; index++)
        {
            var reference = _crossReferences[index];
            if (!cardsByNumber.TryGetValue(reference.CardNumber, out var card)
                || !accountIds.Contains(reference.AccountId)
                || !customerIds.Contains(reference.CustomerId)
                || !card.AccountId.Equals(reference.AccountId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Card cross-reference record {index + 1} is inconsistent.");
            }
        }

        for (var index = 0; index < _transactions.Count; index++)
        {
            if (!cardsByNumber.ContainsKey(_transactions[index].CardNumber))
            {
                throw new InvalidDataException(
                    $"Transaction record {index + 1} references an unknown card.");
            }
        }
    }

    private static void ValidateUnique<T>(
        IReadOnlyList<T> records,
        Func<T, string> keySelector,
        string source)
    {
        if (records.Select(keySelector).Distinct(StringComparer.Ordinal).Count() != records.Count)
        {
            throw new InvalidDataException($"{source} contains duplicate keys.");
        }
    }

    private static LegacyAccount ParseAccount(string line)
    {
        FixedWidth.RequireLength(line, 300, "account");
        return new LegacyAccount(
            FixedWidth.Text(line, 0, 11),
            FixedWidth.Text(line, 11, 1) == "Y",
            FixedWidth.SignedDecimal(line, 12, 12, 2),
            FixedWidth.SignedDecimal(line, 24, 12, 2),
            FixedWidth.SignedDecimal(line, 36, 12, 2),
            FixedWidth.Date(line, 48, 10),
            FixedWidth.Date(line, 58, 10),
            FixedWidth.Date(line, 68, 10),
            FixedWidth.SignedDecimal(line, 78, 12, 2),
            FixedWidth.SignedDecimal(line, 90, 12, 2),
            FixedWidth.Text(line, 102, 10),
            FixedWidth.Text(line, 112, 10));
    }

    private static LegacyCard ParseCard(string line)
    {
        FixedWidth.RequireLength(line, 150, "card");
        return new LegacyCard(
            FixedWidth.Text(line, 0, 16),
            FixedWidth.Text(line, 16, 11),
            FixedWidth.Text(line, 30, 50),
            FixedWidth.Date(line, 80, 10),
            FixedWidth.Text(line, 90, 1) == "Y");
    }

    private static LegacyCustomer ParseCustomer(string line)
    {
        FixedWidth.RequireLength(line, 500, "customer");
        return new LegacyCustomer(
            FixedWidth.Text(line, 0, 9),
            FixedWidth.Text(line, 9, 25),
            FixedWidth.Text(line, 34, 25),
            FixedWidth.Text(line, 59, 25),
            FixedWidth.Text(line, 84, 50),
            FixedWidth.Text(line, 134, 50),
            FixedWidth.Text(line, 184, 50),
            FixedWidth.Text(line, 234, 2),
            FixedWidth.Text(line, 236, 3),
            FixedWidth.Text(line, 239, 10),
            FixedWidth.Text(line, 249, 15),
            FixedWidth.Date(line, 308, 10),
            FixedWidth.Text(line, 328, 1) == "Y",
            FixedWidth.Integer(line, 329, 3));
    }

    private static LegacyTransaction ParseTransaction(string line)
    {
        FixedWidth.RequireLength(line, 350, "transaction");
        return new LegacyTransaction(
            FixedWidth.Text(line, 0, 16),
            FixedWidth.Text(line, 16, 2),
            FixedWidth.Text(line, 18, 4),
            FixedWidth.Text(line, 22, 10),
            FixedWidth.Text(line, 32, 100),
            FixedWidth.SignedDecimal(line, 132, 11, 2),
            FixedWidth.Text(line, 152, 50),
            FixedWidth.Text(line, 202, 50),
            FixedWidth.Text(line, 252, 10),
            FixedWidth.Text(line, 262, 16),
            FixedWidth.Timestamp(line, 278, 26),
            FixedWidth.Timestamp(line, 304, 26));
    }

    private static LegacyCardCrossReference ParseCrossReference(string line)
    {
        // The ASCII export intentionally omits the copybook's 14 trailing filler bytes.
        FixedWidth.RequireLength(line, 36, "card cross-reference ASCII export");
        return new LegacyCardCrossReference(
            FixedWidth.Text(line, 0, 16),
            FixedWidth.Text(line, 25, 11),
            FixedWidth.Text(line, 16, 9));
    }

    private static IEnumerable<string> ReadLines(string dataDirectory, string fileName)
    {
        var path = Path.Combine(dataDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required CardDemo data file '{fileName}' was not found.", path);
        }

        var recordNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            recordNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidDataException(
                    $"{fileName} contains a blank record at position {recordNumber}.");
            }

            yield return line;
        }

        if (recordNumber == 0)
        {
            throw new InvalidDataException($"{fileName} does not contain any records.");
        }
    }

    private static string Pseudonym(LegacyCustomer customer) =>
        $"Sample customer {customer.Id[^4..]}";

    private static string MaskCard(string number) =>
        number.Length < 4 ? "****" : $"•••• •••• •••• {number[^4..]}";
}
