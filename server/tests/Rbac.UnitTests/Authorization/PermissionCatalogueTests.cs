using System.Text.RegularExpressions;
using Rbac.Api.Authorization;

namespace Rbac.UnitTests.Authorization;

public sealed partial class PermissionCatalogueTests
{
    [Fact]
    public void Codes_are_unique()
    {
        var duplicates = Permissions.All.GroupBy(p => p.Code).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Codes_follow_the_resource_dot_action_convention()
    {
        var invalid = Permissions.All.Where(p => !CodeFormat().IsMatch(p.Code)).Select(p => p.Code);

        Assert.Empty(invalid);
    }

    [Fact]
    public void Every_permission_has_a_description()
    {
        Assert.All(Permissions.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Description)));
    }

    [GeneratedRegex("^[a-z]+(\\.[a-z]+)+$")]
    private static partial Regex CodeFormat();
}
