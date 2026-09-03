using CardDemo.Modern.Models;
using CardDemo.Modern.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardDemo.Modern.Pages.Accounts;

public sealed class DetailModel(LegacyDataStore store) : PageModel
{
    public AccountDetail Account { get; private set; } = null!;

    public IActionResult OnGet(string id)
    {
        if (!LegacyDataStore.IsValidAccountId(id))
        {
            return NotFound();
        }

        var account = store.GetAccount(id);
        if (account is null)
        {
            return NotFound();
        }

        Account = account;
        return Page();
    }
}
