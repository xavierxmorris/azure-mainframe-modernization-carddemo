using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardDemo.Modern.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CardDemo.Modern.Tests;

public sealed class AppRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly HashSet<string> ForbiddenJsonProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "cardNumber",
        "cvv",
        "cvvCode",
        "dateOfBirth",
        "embossedName",
        "expirationDate",
        "governmentIssuedId",
        "phone",
        "socialSecurityNumber",
        "ssn"
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AppRoutesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SummaryApi_ReturnsCheckedInDatasetCounts()
    {
        var summary = await _client.GetFromJsonAsync<DashboardSummary>("/api/summary");

        Assert.NotNull(summary);
        Assert.Equal(50, summary.AccountCount);
        Assert.Equal(300, summary.TransactionCount);
    }

    [Fact]
    public async Task PublicResponses_DoNotExposeRawCardOrCustomerValues()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var rawCardNumbers = File.ReadLines(Path.Combine(dataDirectory, "carddata.txt"))
            .Select(line => line[..16])
            .ToArray();
        var rawCustomerName = GetCustomerNameForAccount(dataDirectory, "00000000001");
        var paths = new[]
        {
            "/api/accounts",
            "/api/accounts/00000000001",
            "/Accounts/00000000001"
        };

        var bodies = new List<string>();
        foreach (var path in paths)
        {
            var response = await _client.GetAsync(path);
            response.EnsureSuccessStatusCode();
            bodies.Add(await response.Content.ReadAsStringAsync());
        }

        var leakedCard = rawCardNumbers.Any(card =>
            bodies.Any(body => body.Contains(card, StringComparison.Ordinal)));
        var leakedCustomerName = bodies.Any(body =>
            body.Contains(rawCustomerName, StringComparison.OrdinalIgnoreCase));

        Assert.False(leakedCard, "A public response contained a raw card number.");
        Assert.False(leakedCustomerName, "A public response contained a source customer name.");

        using var listJson = JsonDocument.Parse(bodies[0]);
        using var detailJson = JsonDocument.Parse(bodies[1]);
        Assert.False(
            ContainsForbiddenProperty(listJson.RootElement)
            || ContainsForbiddenProperty(detailJson.RootElement),
            "A public JSON contract contained a forbidden sensitive-field property.");
    }

    [Fact]
    public async Task AccountApi_ReturnsMaskedCards()
    {
        var account = await _client.GetFromJsonAsync<AccountDetail>("/api/accounts/00000000001");

        Assert.NotNull(account);
        Assert.NotEmpty(account.Cards);
        Assert.All(account.Cards, card =>
            Assert.StartsWith("•••• •••• •••• ", card.MaskedNumber, StringComparison.Ordinal));
    }

    [Fact]
    public async Task HtmlResponse_HasBrowserSecurityHeaders()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task ProductionResponse_HasHsts()
    {
        using var client = _factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task UnknownAccount_ReturnsSafeNotFoundPage()
    {
        var response = await _client.GetAsync("/Accounts/99999999999");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Record path not found", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Development Mode", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/accounts/4111111111111111")]
    [InlineData("/api/accounts/not-an-id")]
    [InlineData("/api/accounts/1")]
    public async Task AccountApi_RejectsValuesThatAreNotAccountIdentifiers(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AccountApi_DoesNotEchoRejectedCardShapedInput()
    {
        const string pan = "4111111111111111";

        var response = await _client.GetAsync($"/api/accounts/{pan}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(pan, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownButWellFormedAccount_DoesNotEchoTheIdentifier()
    {
        var response = await _client.GetAsync("/api/accounts/99999999999");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("99999999999", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchApi_RejectsTermsWiderThanAnAccountIdentifier()
    {
        const string pan = "4111111111111111";

        var response = await _client.GetAsync($"/api/accounts?q={pan}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(pan, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchPage_DoesNotEchoRejectedInput()
    {
        const string pan = "4111111111111111";

        var response = await _client.GetAsync($"/Accounts?query={pan}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(pan, body, StringComparison.Ordinal);
    }

    private static bool ContainsForbiddenProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ForbiddenJsonProperties.Contains(property.Name)
                    || ContainsForbiddenProperty(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(ContainsForbiddenProperty);
        }

        return false;
    }

    private static string GetCustomerNameForAccount(string dataDirectory, string accountId)
    {
        var crossReference = File.ReadLines(Path.Combine(dataDirectory, "cardxref.txt"))
            .Single(line => line.Substring(25, 11) == accountId);
        var customerId = crossReference.Substring(16, 9);
        var customer = File.ReadLines(Path.Combine(dataDirectory, "custdata.txt"))
            .Single(line => line.StartsWith(customerId, StringComparison.Ordinal));

        return string.Join(
            " ",
            new[]
            {
                customer.Substring(9, 25).Trim(),
                customer.Substring(34, 25).Trim(),
                customer.Substring(59, 25).Trim()
            }.Where(value => value.Length > 0));
    }
}
