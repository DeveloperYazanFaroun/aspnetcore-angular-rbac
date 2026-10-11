using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rbac.Api.Authorization;

/// <summary>
/// Runs before the action. Returns 401 when nobody is signed in and 403 when the permission is missing.
/// Created by <see cref="HasPermissionAttribute"/>, which passes the required permission.
/// </summary>
public sealed class PermissionAuthorizationFilter(
    string permission,
    IPermissionService permissionService,
    ILogger<PermissionAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = Problem(
                StatusCodes.Status401Unauthorized, "Unauthorized", "You need to sign in to access this resource.");
            return;
        }

        var subject = user.FindFirstValue(AppClaimTypes.Subject);
        var hasUserId = Guid.TryParse(subject, out var userId);

        if (!hasUserId || !await permissionService.HasPermissionAsync(userId, permission, context.HttpContext.RequestAborted))
        {
            logger.LogWarning(
                "Access denied: user {UserId} lacks permission {Permission} for {Method} {Path}",
                subject, permission, context.HttpContext.Request.Method, context.HttpContext.Request.Path);

            context.Result = Problem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"You don't have the '{permission}' permission required for this action.");
        }
    }

    private static ObjectResult Problem(int status, string title, string detail) =>
        new(new ProblemDetails { Status = status, Title = title, Detail = detail }) { StatusCode = status, ContentTypes = { "application/problem+json" } };
}
