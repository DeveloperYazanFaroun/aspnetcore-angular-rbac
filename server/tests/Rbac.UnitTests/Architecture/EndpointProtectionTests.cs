using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;

namespace Rbac.UnitTests.Architecture;

/// <summary>
/// Fails the build when someone adds an endpoint and forgets to decide who may call it.
/// Every action must have [HasPermission], [AuthenticatedOnly] or an explicit [AllowAnonymous].
/// </summary>
public sealed class EndpointProtectionTests
{
    [Fact]
    public void Every_endpoint_declares_its_authorization()
    {
        var unprotected = typeof(Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(action => !IsProtected(controller, action))
                .Select(action => $"{controller.Name}.{action.Name}"))
            .ToList();

        Assert.True(unprotected.Count == 0, "Endpoints without an authorization decision: " + string.Join(", ", unprotected));
    }

    private static bool IsProtected(Type controller, MethodInfo action) =>
        Has<HasPermissionAttribute>(action) || Has<HasPermissionAttribute>(controller) ||
        Has<AuthenticatedOnlyAttribute>(action) || Has<AuthenticatedOnlyAttribute>(controller) ||
        Has<AllowAnonymousAttribute>(action) || Has<AllowAnonymousAttribute>(controller);

    private static bool Has<TAttribute>(MemberInfo member) where TAttribute : Attribute =>
        member.IsDefined(typeof(TAttribute), inherit: true);
}
