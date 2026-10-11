using System.Security.Claims;

namespace Rbac.Api.Authorization;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ReadGuid(AppClaimTypes.Subject);

    public Guid? TenantId => ReadGuid(AppClaimTypes.TenantId);

    public Guid RequiredUserId =>
        UserId ?? throw new InvalidOperationException("This operation requires a signed-in user.");

    public Guid RequiredTenantId =>
        TenantId ?? throw new InvalidOperationException("This operation requires a signed-in user with a tenant.");

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
