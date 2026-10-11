using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;
using Rbac.Api.Common;

namespace Rbac.Api.Features.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        userService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<ActionResult<UserResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await userService.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<UserResponse>> AssignRoles(
        Guid id, AssignRolesRequest request, CancellationToken cancellationToken) =>
        (await userService.AssignRolesAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<UserResponse>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        (await userService.DeactivateAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<UserResponse>> Activate(Guid id, CancellationToken cancellationToken) =>
        (await userService.ActivateAsync(id, cancellationToken)).ToActionResult(this);
}
