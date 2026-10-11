using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rbac.Api.Features.Auth;

namespace Rbac.IntegrationTests.Infrastructure;

public static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Signs in as a demo user and returns a client that sends the access token.</summary>
    public static async Task<HttpClient> CreateClientForAsync(this RbacApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException($"Response body was empty ({(int)response.StatusCode}).");
    }
}
