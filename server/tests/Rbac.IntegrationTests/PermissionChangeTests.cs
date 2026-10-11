using System.Net;
using System.Net.Http.Json;
using Rbac.Api.Features.Roles;
using Rbac.Api.Features.Users;
using Rbac.IntegrationTests.Infrastructure;

namespace Rbac.IntegrationTests;

/// <summary>
/// Permissions are resolved on the server, so changes apply on the next request,
/// without waiting for the user's access token to expire.
/// </summary>
public sealed class PermissionChangeTests(RbacApiFactory factory) : IClassFixture<RbacApiFactory>
{
    [Fact]
    public async Task Removing_a_permission_from_a_role_applies_immediately()
    {
        var viewer = await factory.CreateClientForAsync(TestUsers.ContosoViewer);
        var admin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/invoices")).StatusCode);

        var roles = await (await admin.GetAsync("/api/roles")).ReadAsync<List<RoleResponse>>();
        var viewerRole = roles.Single(r => r.Name == "Viewer");
        var update = await admin.PutAsJsonAsync($"/api/roles/{viewerRole.Id}",
            new { name = viewerRole.Name, description = viewerRole.Description, permissions = Array.Empty<string>() });
        update.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/invoices")).StatusCode);
    }

    [Fact]
    public async Task Deactivated_user_loses_access_immediately()
    {
        var accountant = await factory.CreateClientForAsync(TestUsers.ContosoAccountant);
        var admin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);
        Assert.Equal(HttpStatusCode.OK, (await accountant.GetAsync("/api/invoices")).StatusCode);

        var users = await (await admin.GetAsync("/api/users")).ReadAsync<List<UserResponse>>();
        var target = users.Single(u => u.Email == TestUsers.ContosoAccountant);
        (await admin.PostAsync($"/api/users/{target.Id}/deactivate", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync("/api/invoices")).StatusCode);
    }

    [Fact]
    public async Task Users_cannot_change_their_own_roles()
    {
        var admin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);
        var users = await (await admin.GetAsync("/api/users")).ReadAsync<List<UserResponse>>();
        var self = users.Single(u => u.Email == TestUsers.ContosoAdmin);

        var response = await admin.PutAsJsonAsync($"/api/users/{self.Id}/roles", new { roleIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
