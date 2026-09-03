using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardDemo.Modern.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? HttpStatusCode { get; set; }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string Heading => HttpStatusCode == StatusCodes.Status404NotFound
        ? "Record path not found"
        : "The request could not be completed";
    public string Message => HttpStatusCode == StatusCodes.Status404NotFound
        ? "The requested page or account does not exist in this reference slice."
        : "No source data was changed. Use the request ID when reviewing application logs.";

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
