namespace Rbac.Api.Domain;

/// <summary>
/// An organization that uses the application. Users, roles and business data are isolated per tenant.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;
}
