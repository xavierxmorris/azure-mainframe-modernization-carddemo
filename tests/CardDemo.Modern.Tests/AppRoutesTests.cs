using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardDemo.Modern.Models;
using CardDemo.Modern.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CardDemo.Modern.Tests;

public sealed class AppRoutesTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
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
    private readonly Uri? _liveTarget;

    public AppRoutesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _liveTarget = ReadLiveTarget();
        _client = _liveTarget is null ? factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        }) : CreateLiveClient(_liveTarget);
    }

    [Fact]
    public async Task SummaryApi_ReturnsCheckedInDatasetCounts()
    {
        var summary = await _client.GetFromJsonAsync<DashboardSummary>("/api/summary");

        Assert.NotNull(summary);
        Assert.Equal(50, summary.AccountCount);
        Assert.Equal(300, summary.TransactionCount);
        Assert.Equal(new LegacyDataStore().Summary, summary);
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
        using var client = _liveTarget is not null ? CreateLiveClient(_liveTarget) : _factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task EveryAccount_MatchesTheLocalReadOnlyReference()
    {
        var reference = new LegacyDataStore();
        var expectedList = reference.SearchAccounts();
        Assert.Equal(50, expectedList.Count);
        var actualList = await _client.GetFromJsonAsync<AccountListItem[]>("/api/accounts");
        Assert.NotNull(actualList);
        Assert.Equal(expectedList, actualList);

        var rawCards = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Data", "carddata.txt"))
            .Select(line => line[..16]).ToArray();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var transactionCount = 0;
        foreach (var item in expectedList)
        {
            var response = await _client.GetAsync($"/api/accounts/{item.Id}");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.False(ContainsForbiddenProperty(json.RootElement), "A detail exposed a forbidden property.");
            Assert.False(rawCards.Any(card => body.Contains(card, StringComparison.Ordinal)),
                "A detail exposed a raw card number.");

            var actual = JsonSerializer.Deserialize<AccountDetail>(body, jsonOptions);
            var expected = reference.GetAccount(item.Id);
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.Account, actual.Account);
            Assert.Equal(expected.Cards, actual.Cards);
            Assert.Equal(expected.Customers, actual.Customers);
            Assert.Equal(expected.Transactions, actual.Transactions);
            Assert.Equal(item.CardCount, actual.Cards.Count);
            Assert.Equal(item.TransactionCount, actual.Transactions.Count);
            transactionCount += actual.Transactions.Count;
        }
        Assert.Equal(reference.Summary.TransactionCount, transactionCount);
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

    private static Uri? ReadLiveTarget()
    {
        var configured = Environment.GetEnvironmentVariable("CARDDEMO_TEST_BASE_URL");
        if (configured is null)
        {
            return null;
        }
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            || !(uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                "CARDDEMO_TEST_BASE_URL must be an HTTPS origin or loopback HTTP origin, without credentials.");
        }
        return uri;
    }

    private static HttpClient CreateLiveClient(Uri target) => new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        BaseAddress = target,
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 2 * 1024 * 1024
    };

    public void Dispose() => _client.Dispose();
}
