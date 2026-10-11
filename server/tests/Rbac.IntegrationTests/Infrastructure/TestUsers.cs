namespace Rbac.IntegrationTests.Infrastructure;

/// <summary>Demo accounts created by the database seeder.</summary>
public static class TestUsers
{
    public const string Password = "Passw0rd!";

    public const string ContosoAdmin = "admin@contoso.test";
    public const string ContosoAccountant = "accountant@contoso.test";
    public const string ContosoApprover = "approver@contoso.test";
    public const string ContosoViewer = "viewer@contoso.test";

    /// <summary>Has both Accountant and Approver roles: used to test separation of duties.</summary>
    public const string ContosoLead = "lead@contoso.test";

    public const string FabrikamAccountant = "accountant@fabrikam.test";
}
