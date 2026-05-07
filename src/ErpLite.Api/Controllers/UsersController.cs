using ErpLite.Api.Extensions;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] IValidator<RegisterUserRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await userService.RegisterAsync(request, cancellationToken);
        return result.Succeeded ? Created($"/api/users/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleToUserRequest request,
        [FromServices] IValidator<AssignRoleToUserRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await userService.AssignRoleAsync(id, request, cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { error = result.Error });
    }
}
