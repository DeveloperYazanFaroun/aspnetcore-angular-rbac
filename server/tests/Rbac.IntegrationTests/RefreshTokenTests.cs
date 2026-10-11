using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Rbac.Api.Features.Auth;
using Rbac.IntegrationTests.Infrastructure;

namespace Rbac.IntegrationTests;

public sealed class RefreshTokenTests(RbacApiFactory factory) : IClassFixture<RbacApiFactory>
{
    [Fact]
    public async Task Refresh_token_is_rotated_and_reuse_revokes_the_session()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestUsers.ContosoAccountant, password = TestUsers.Password });
        var first = ReadRefreshCookie(login);

        var refreshed = await RefreshAsync(client, first);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = ReadRefreshCookie(refreshed);
        Assert.NotEqual(first, second);

        // Using the old token again looks like theft: every session of the user is revoked.
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, first)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, second)).StatusCode);
    }

    [Fact]
    public async Task Refresh_cookie_is_http_only_and_strict()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestUsers.ContosoViewer, password = TestUsers.Password });

        var cookie = login.Headers.GetValues("Set-Cookie").Single(h => h.StartsWith(AuthController.RefreshCookieName));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Wrong_password_returns_401_with_a_generic_message()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = TestUsers.ContosoViewer, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={refreshToken}");
        return client.SendAsync(request);
    }

    private static string ReadRefreshCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(h => h.StartsWith(AuthController.RefreshCookieName + "="));
        var value = header[(AuthController.RefreshCookieName.Length + 1)..];
        return value[..value.IndexOf(';')];
    }
}
