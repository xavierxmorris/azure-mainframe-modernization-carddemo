using CardDemo.Modern.Services;
using System.Globalization;
using System.Threading.RateLimiting;

var defaultCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<LegacyDataStore>();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter(
            "global",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();
var dataStore = app.Services.GetRequiredService<LegacyDataStore>();
app.Logger.LogInformation(
    "Loaded CardDemo reference data: {Accounts} accounts, {Cards} cards, {Transactions} transactions.",
    dataStore.Summary.AccountCount,
    dataStore.Summary.CardCount,
    dataStore.Summary.TransactionCount);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStatusCodePagesWithReExecute("/Error", "?httpStatusCode={0}");
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";
    }

    await next();
});
app.UseResponseCompression();
app.UseRouting();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapHealthChecks("/health").DisableRateLimiting();
app.MapGet("/api/summary", (LegacyDataStore store) => Results.Ok(store.Summary));
app.MapGet("/api/accounts", (string? q, LegacyDataStore store) =>
    LegacyDataStore.IsValidSearchTerm(q)
        ? Results.Ok(store.SearchAccounts(q))
        : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["q"] = ["Search terms may contain at most 11 digits."]
        }));
app.MapGet("/api/accounts/{accountId}", (string accountId, LegacyDataStore store) =>
{
    if (!LegacyDataStore.IsValidAccountId(accountId))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["accountId"] = ["Account identifiers must be exactly 11 digits."]
        });
    }

    return store.GetAccount(accountId) is { } account
        ? Results.Ok(account)
        : Results.NotFound(new { message = "The requested account was not found." });
});

app.Run();

public partial class Program;
