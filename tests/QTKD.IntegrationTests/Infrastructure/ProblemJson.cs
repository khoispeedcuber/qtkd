using System.Net.Http.Json;
using System.Text.Json;

namespace QTKD.IntegrationTests.Infrastructure;

public sealed record ProblemJson(string? Rule, IReadOnlyList<string> ErrorFields)
{
    public static async Task<ProblemJson> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var root = await response.Content.ReadFromJsonAsync<JsonElement>();

        var rule = root.TryGetProperty("rule", out var r) ? r.GetString() : null;
        var fields = root.TryGetProperty("errors", out var e)
            ? e.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList()
            : new List<string>();

        return new ProblemJson(rule, fields);
    }
}
