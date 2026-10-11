using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Rbac.Api.Authorization;

namespace Rbac.UnitTests.Authorization;

public sealed class PermissionAuthorizationFilterTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Returns_401_when_nobody_is_signed_in()
    {
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

        await CreateFilter(Permissions.Invoices.View).OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Returns_403_when_the_permission_is_missing()
    {
        var context = CreateContext(SignedInUser());

        await CreateFilter(Permissions.Invoices.Approve, Permissions.Invoices.View).OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Contains(Permissions.Invoices.Approve, problem.Detail);
    }

    [Fact]
    public async Task Lets_the_request_continue_when_the_permission_is_granted()
    {
        var context = CreateContext(SignedInUser());

        await CreateFilter(Permissions.Invoices.View, Permissions.Invoices.View).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Skips_the_check_for_anonymous_endpoints()
    {
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()), new AllowAnonymousAttribute());

        await CreateFilter(Permissions.Invoices.View).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static PermissionAuthorizationFilter CreateFilter(string permission, params string[] granted) =>
        new(permission, new FakePermissionService(granted), NullLogger<PermissionAuthorizationFilter>.Instance);

    private static ClaimsPrincipal SignedInUser() =>
        new(new ClaimsIdentity(new[] { new Claim(AppClaimTypes.Subject, UserId.ToString()) }, authenticationType: "Test"));

    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal user, params object[] endpointMetadata)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata.ToList() };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private sealed class FakePermissionService(params string[] granted) : IPermissionService
    {
        private readonly HashSet<string> _granted = granted.ToHashSet();

        public Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(_granted);

        public Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default) =>
            Task.FromResult(_granted.Contains(permission));

        public void Invalidate(IEnumerable<Guid> userIds)
        {
        }
    }
}
