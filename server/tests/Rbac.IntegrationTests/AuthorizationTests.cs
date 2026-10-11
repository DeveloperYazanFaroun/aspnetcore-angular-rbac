using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;
using Rbac.Api.Domain;
using Rbac.Api.Features.Invoices;
using Rbac.Api.Features.Me;
using Rbac.Api.Features.Roles;
using Rbac.IntegrationTests.Infrastructure;

namespace Rbac.IntegrationTests;

/// <summary>
/// End-to-end checks of the four authorization layers:
/// authentication (401), permissions (403), tenant isolation (404) and business rules.
/// </summary>
public sealed class AuthorizationTests(RbacApiFactory factory) : IClassFixture<RbacApiFactory>
{
    [Fact]
    public async Task Request_without_a_token_returns_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/invoices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_create_an_invoice()
    {
        var viewer = await factory.CreateClientForAsync(TestUsers.ContosoViewer);

        var response = await viewer.PostAsJsonAsync("/api/invoices", new { customerName = "Fourth Coffee", amount = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Contains(Permissions.Invoices.Create, problem.Detail);
    }

    [Fact]
    public async Task Accountant_can_create_an_invoice()
    {
        var accountant = await factory.CreateClientForAsync(TestUsers.ContosoAccountant);

        var response = await accountant.PostAsJsonAsync("/api/invoices", new { customerName = "Fourth Coffee", amount = 10 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.ReadAsync<InvoiceResponse>();
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public async Task Users_cannot_see_invoices_of_another_tenant()
    {
        var fabrikam = await factory.CreateClientForAsync(TestUsers.FabrikamAccountant);
        var fabrikamInvoice = (await (await fabrikam.GetAsync("/api/invoices")).ReadAsync<List<InvoiceResponse>>()).First();

        var contosoAdmin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);
        var single = await contosoAdmin.GetAsync($"/api/invoices/{fabrikamInvoice.Id}");
        var list = await (await contosoAdmin.GetAsync("/api/invoices")).ReadAsync<List<InvoiceResponse>>();

        Assert.Equal(HttpStatusCode.NotFound, single.StatusCode);
        Assert.DoesNotContain(list, i => i.Id == fabrikamInvoice.Id);
    }

    [Fact]
    public async Task Creator_cannot_approve_their_own_invoice()
    {
        var lead = await factory.CreateClientForAsync(TestUsers.ContosoLead);
        var invoice = await CreateSubmittedInvoiceAsync(lead);

        var response = await lead.PostAsync($"/api/invoices/{invoice.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Contains("you created", problem.Detail);
    }

    [Fact]
    public async Task Another_approver_can_approve_the_invoice()
    {
        var lead = await factory.CreateClientForAsync(TestUsers.ContosoLead);
        var invoice = await CreateSubmittedInvoiceAsync(lead);
        var approver = await factory.CreateClientForAsync(TestUsers.ContosoApprover);

        var response = await approver.PostAsync($"/api/invoices/{invoice.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InvoiceStatus.Approved, (await response.ReadAsync<InvoiceResponse>()).Status);
    }

    [Fact]
    public async Task Export_requires_both_view_and_export_permissions()
    {
        var approver = await factory.CreateClientForAsync(TestUsers.ContosoApprover);
        var accountant = await factory.CreateClientForAsync(TestUsers.ContosoAccountant);

        var denied = await approver.GetAsync("/api/invoices/export");
        var allowed = await accountant.GetAsync("/api/invoices/export");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("text/csv", allowed.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Me_returns_the_effective_permissions()
    {
        var viewer = await factory.CreateClientForAsync(TestUsers.ContosoViewer);

        var me = await (await viewer.GetAsync("/api/me")).ReadAsync<MeResponse>();

        Assert.Equal(new[] { Permissions.Invoices.View }, me.Permissions);
        Assert.Equal(new[] { "Viewer" }, me.Roles);
    }

    [Fact]
    public async Task Duplicate_role_name_returns_a_readable_conflict()
    {
        var admin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);

        var response = await admin.PostAsJsonAsync("/api/roles",
            new { name = "Viewer", description = "Copy", permissions = new[] { Permissions.Invoices.View } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal("A role named 'Viewer' already exists. Choose a different role name.", problem.Detail);
    }

    [Fact]
    public async Task Users_cannot_grant_permissions_they_do_not_have()
    {
        var admin = await factory.CreateClientForAsync(TestUsers.ContosoAdmin);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // A "role manager" can manage roles but has no invoice permissions.
        var roleResponse = await admin.PostAsJsonAsync("/api/roles", new
        {
            name = $"Role Manager {suffix}",
            description = "Manages roles only",
            permissions = new[] { Permissions.Roles.View, Permissions.Roles.Manage },
        });
        var role = await roleResponse.ReadAsync<RoleResponse>();

        var email = $"role-manager-{suffix}@contoso.test";
        var userResponse = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Role Manager",
            email,
            password = TestUsers.Password,
            roleIds = new[] { role.Id },
        });
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);

        var roleManager = await factory.CreateClientForAsync(email);
        var escalation = await roleManager.PostAsJsonAsync("/api/roles", new
        {
            name = $"Sneaky {suffix}",
            description = "Tries to grant approval",
            permissions = new[] { Permissions.Invoices.Approve },
        });

        Assert.Equal(HttpStatusCode.Forbidden, escalation.StatusCode);
        var problem = await escalation.ReadAsync<ProblemDetails>();
        Assert.Contains(Permissions.Invoices.Approve, problem.Detail);
    }

    private static async Task<InvoiceResponse> CreateSubmittedInvoiceAsync(HttpClient client)
    {
        var created = await (await client.PostAsJsonAsync("/api/invoices", new { customerName = "Contoso Pharmacy", amount = 250 }))
            .ReadAsync<InvoiceResponse>();

        var submitted = await client.PostAsync($"/api/invoices/{created.Id}/submit", null);
        submitted.EnsureSuccessStatusCode();
        return created;
    }
}
