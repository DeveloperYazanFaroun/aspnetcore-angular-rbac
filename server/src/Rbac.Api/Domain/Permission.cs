namespace Rbac.Api.Domain;

/// <summary>
/// One allowed action, for example "invoices.approve".
/// Rows are synchronized from <see cref="Authorization.Permissions"/> at startup; never edit them by hand.
/// </summary>
public sealed class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
