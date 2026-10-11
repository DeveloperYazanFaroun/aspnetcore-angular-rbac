namespace Rbac.Api.Domain;

/// <summary>
/// Marks business data that belongs to exactly one tenant.
/// Every entity that implements this interface must get a tenant query filter in <c>AppDbContext</c>.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; }
}
