using System.ComponentModel;
using System.Reflection;

namespace Rbac.Api.Authorization;

/// <summary>
/// The permission catalogue: every action the application can protect.
/// <para>
/// Permissions are defined by developers, in code, and never change meaning.
/// Roles are defined by administrators, as data, and group these permissions.
/// Code checks permissions, never role names.
/// </para>
/// Naming rule: <c>resource.action</c>, lower case. Keep <c>client/src/app/core/auth/permissions.ts</c> in sync.
/// </summary>
public static class Permissions
{
    public static class Invoices
    {
        [Description("View invoices")]
        public const string View = "invoices.view";

        [Description("Create draft invoices")]
        public const string Create = "invoices.create";

        [Description("Edit draft invoices")]
        public const string Edit = "invoices.edit";

        [Description("Delete draft invoices")]
        public const string Delete = "invoices.delete";

        [Description("Submit draft invoices for approval")]
        public const string Submit = "invoices.submit";

        [Description("Approve or reject submitted invoices")]
        public const string Approve = "invoices.approve";

        [Description("Export invoices to CSV")]
        public const string Export = "invoices.export";
    }

    public static class Users
    {
        [Description("View users in the organization")]
        public const string View = "users.view";

        [Description("Create users, assign roles, activate and deactivate users")]
        public const string Manage = "users.manage";
    }

    public static class Roles
    {
        [Description("View roles and their permissions")]
        public const string View = "roles.view";

        [Description("Create, edit and delete roles")]
        public const string Manage = "roles.manage";
    }

    /// <summary>
    /// All permissions, discovered once by reflection over the nested classes above.
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } = Discover();

    private static List<PermissionDefinition> Discover() =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(group => group
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => new PermissionDefinition(
                    Code: (string)field.GetRawConstantValue()!,
                    Group: group.Name,
                    Description: field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? field.Name)))
            .ToList();
}

public sealed record PermissionDefinition(string Code, string Group, string Description);
