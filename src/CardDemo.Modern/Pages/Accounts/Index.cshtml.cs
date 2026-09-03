using CardDemo.Modern.Models;
using CardDemo.Modern.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardDemo.Modern.Pages.Accounts;

public sealed class IndexModel(LegacyDataStore store) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public IReadOnlyList<AccountListItem> Accounts { get; private set; } = [];

    public string? ValidationMessage { get; private set; }

    public void OnGet()
    {
        // Reject anything wider than an account identifier so a pasted card number is
        // never echoed back into the page or written to request logs.
        if (!LegacyDataStore.IsValidSearchTerm(Query))
        {
            Query = null;
            ValidationMessage = "Search terms may contain at most 11 digits.";
            Accounts = [];
            return;
        }

        Accounts = store.SearchAccounts(Query);
    }
}
