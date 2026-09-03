namespace CardDemo.Modern.Models;

internal sealed record LegacyAccount(
    string Id,
    bool IsActive,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal CashCreditLimit,
    DateOnly? OpenDate,
    DateOnly? ExpirationDate,
    DateOnly? ReissueDate,
    decimal CurrentCycleCredit,
    decimal CurrentCycleDebit,
    string PostalCode,
    string GroupId);

internal sealed record LegacyCard(
    string Number,
    string AccountId,
    string EmbossedName,
    DateOnly? ExpirationDate,
    bool IsActive);

internal sealed record LegacyCustomer(
    string Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string AddressLine1,
    string AddressLine2,
    string AddressLine3,
    string State,
    string Country,
    string PostalCode,
    string Phone,
    DateOnly? DateOfBirth,
    bool IsPrimaryCardHolder,
    int CreditScore);

internal sealed record LegacyTransaction(
    string Id,
    string TypeCode,
    string CategoryCode,
    string Source,
    string Description,
    decimal Amount,
    string MerchantName,
    string MerchantCity,
    string MerchantPostalCode,
    string CardNumber,
    DateTime? OriginalTimestamp,
    DateTime? ProcessedTimestamp);

internal sealed record LegacyCardCrossReference(string CardNumber, string AccountId, string CustomerId);

public sealed record AccountListItem(
    string Id,
    bool IsActive,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal CashCreditLimit,
    int CardCount,
    int TransactionCount,
    string CardholderLabel);

public sealed record CardView(
    string MaskedNumber,
    string CardholderLabel,
    bool IsActive);

public sealed record CustomerView(
    string DisplayLabel,
    string Location,
    bool IsPrimaryCardHolder);

public sealed record TransactionView(
    string Id,
    string TypeCode,
    string CategoryCode,
    string Source,
    string Description,
    decimal Amount,
    string MerchantName,
    string MerchantCity,
    DateTime? OriginalTimestamp);

public sealed record AccountDetail(
    AccountListItem Account,
    IReadOnlyList<CardView> Cards,
    IReadOnlyList<CustomerView> Customers,
    IReadOnlyList<TransactionView> Transactions);

public sealed record DashboardSummary(
    int AccountCount,
    int ActiveAccountCount,
    int CardCount,
    int CustomerCount,
    int TransactionCount);
