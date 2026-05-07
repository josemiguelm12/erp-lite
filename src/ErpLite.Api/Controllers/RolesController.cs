using ErpLite.Api.Extensions;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        [FromServices] IValidator<CreateRoleRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await roleService.CreateAsync(request, cancellationToken);
        return result.Succeeded ? Created($"/api/roles/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermission(
        Guid id,
        [FromBody] AssignPermissionToRoleRequest request,
        [FromServices] IValidator<AssignPermissionToRoleRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await roleService.AssignPermissionAsync(id, request, cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { error = result.Error });
    }
}
