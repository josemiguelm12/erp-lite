using ErpLite.Api.Extensions;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterTenantRequest request,
        [FromServices] IValidator<RegisterTenantRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await authService.RegisterTenantAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        [FromServices] IValidator<LoginRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }
}
