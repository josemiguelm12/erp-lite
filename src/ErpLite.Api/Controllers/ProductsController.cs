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
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetPagedAsync(page, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        [FromServices] IValidator<CreateProductRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await productService.CreateAsync(request, cancellationToken);
        return result.Status == OperationStatus.Success
            ? Created($"/api/products/{result.Value!.Id}", result.Value)
            : result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        [FromServices] IValidator<UpdateProductRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await productService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
