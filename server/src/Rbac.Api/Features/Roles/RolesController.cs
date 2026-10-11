using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;
using Rbac.Api.Common;

namespace Rbac.Api.Features.Roles;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(RoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public Task<IReadOnlyList<RoleResponse>> List(CancellationToken cancellationToken) =>
        roleService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<RoleResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await roleService.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleResponse>> Create(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await roleService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleResponse>> Update(Guid id, SaveRoleRequest request, CancellationToken cancellationToken) =>
        (await roleService.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await roleService.DeleteAsync(id, cancellationToken)).ToActionResult(this);
}
