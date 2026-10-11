namespace Rbac.Api.Authorization;

/// <summary>
/// The signed-in user for the current request. Read claims through this interface
/// instead of calling <c>HttpContext.User.FindFirst(...)</c> all over the code.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? TenantId { get; }

    /// <summary>The user id, or an exception when no user is signed in.</summary>
    Guid RequiredUserId { get; }

    /// <summary>The tenant id, or an exception when no user is signed in.</summary>
    Guid RequiredTenantId { get; }
}
