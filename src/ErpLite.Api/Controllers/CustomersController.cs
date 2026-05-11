using ErpLite.Api.Extensions;
using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await customerService.GetPagedAsync(page, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await customerService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        [FromServices] IValidator<CreateCustomerRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await customerService.CreateAsync(request, cancellationToken);
        return result.Status == OperationStatus.Success
            ? Created($"/api/customers/{result.Value!.Id}", result.Value)
            : result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        [FromServices] IValidator<UpdateCustomerRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await customerService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await customerService.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
