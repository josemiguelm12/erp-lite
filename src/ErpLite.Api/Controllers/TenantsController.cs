using ErpLite.Api.Extensions;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantService tenantService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<TenantResponse>> Create(
        CreateTenantRequest request,
        IValidator<CreateTenantRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await tenantService.CreateAsync(request, cancellationToken);
        return result.Succeeded ? Created($"/api/tenants/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }
}
