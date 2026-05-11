using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class ProductService(
    IProductRepository products,
    ITenantProvider tenantProvider,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork) : IProductService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public async Task<OperationResult<PagedResponse<ProductResponse>>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("products.read", cancellationToken))
        {
            return OperationResult<PagedResponse<ProductResponse>>.Forbidden("Permission products.read is required.");
        }

        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var (items, totalCount) = await products.GetPagedAsync(normalizedPage, normalizedPageSize, search, cancellationToken);
        var response = new PagedResponse<ProductResponse>(
            items.Select(ToResponse).ToArray(),
            totalCount,
            normalizedPage,
            normalizedPageSize,
            CalculateTotalPages(totalCount, normalizedPageSize));

        return OperationResult<PagedResponse<ProductResponse>>.Success(response);
    }

    public async Task<OperationResult<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("products.read", cancellationToken))
        {
            return OperationResult<ProductResponse>.Forbidden("Permission products.read is required.");
        }

        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null
            ? OperationResult<ProductResponse>.NotFound("Product not found.")
            : OperationResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<OperationResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("products.create", cancellationToken))
        {
            return OperationResult<ProductResponse>.Forbidden("Permission products.create is required.");
        }

        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return OperationResult<ProductResponse>.Forbidden("Tenant context is required.");
        }

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            Stock = request.Stock
        };

        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<OperationResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("products.update", cancellationToken))
        {
            return OperationResult.Forbidden("Permission products.update is required.");
        }

        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return OperationResult.NotFound("Product not found.");
        }

        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.Price = request.Price;
        product.Stock = request.Stock;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("products.delete", cancellationToken))
        {
            return OperationResult.Forbidden("Permission products.delete is required.");
        }

        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return OperationResult.NotFound("Product not found.");
        }

        product.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CreatedAt, product.CreatedBy, product.UpdatedAt, product.UpdatedBy);

    private static int NormalizePage(int page) => page < DefaultPage ? DefaultPage : page;

    private static int NormalizePageSize(int pageSize) => pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

    private static int CalculateTotalPages(int totalCount, int pageSize) => totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
