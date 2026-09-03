using CardDemo.Modern.Models;
using CardDemo.Modern.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardDemo.Modern.Pages;

public sealed class IndexModel(LegacyDataStore store) : PageModel
{
    public DashboardSummary Summary { get; } = store.Summary;
    public IReadOnlyList<AccountListItem> Accounts { get; } = store.SearchAccounts().Take(5).ToArray();
}
