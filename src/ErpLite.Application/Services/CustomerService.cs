using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class CustomerService(
    ICustomerRepository customers,
    ITenantProvider tenantProvider,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork) : ICustomerService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public async Task<OperationResult<PagedResponse<CustomerResponse>>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("customers.read", cancellationToken))
        {
            return OperationResult<PagedResponse<CustomerResponse>>.Forbidden("Permission customers.read is required.");
        }

        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var (items, totalCount) = await customers.GetPagedAsync(normalizedPage, normalizedPageSize, search, cancellationToken);
        var response = new PagedResponse<CustomerResponse>(
            items.Select(ToResponse).ToArray(),
            totalCount,
            normalizedPage,
            normalizedPageSize,
            CalculateTotalPages(totalCount, normalizedPageSize));

        return OperationResult<PagedResponse<CustomerResponse>>.Success(response);
    }

    public async Task<OperationResult<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("customers.read", cancellationToken))
        {
            return OperationResult<CustomerResponse>.Forbidden("Permission customers.read is required.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        return customer is null
            ? OperationResult<CustomerResponse>.NotFound("Customer not found.")
            : OperationResult<CustomerResponse>.Success(ToResponse(customer));
    }

    public async Task<OperationResult<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("customers.create", cancellationToken))
        {
            return OperationResult<CustomerResponse>.Forbidden("Permission customers.create is required.");
        }

        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return OperationResult<CustomerResponse>.Forbidden("Tenant context is required.");
        }

        var customer = new Customer
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Email = NormalizeOptional(request.Email?.ToLowerInvariant()),
            Phone = NormalizeOptional(request.Phone),
            Address = NormalizeOptional(request.Address)
        };

        await customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<CustomerResponse>.Success(ToResponse(customer));
    }

    public async Task<OperationResult> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("customers.update", cancellationToken))
        {
            return OperationResult.Forbidden("Permission customers.update is required.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return OperationResult.NotFound("Customer not found.");
        }

        customer.Name = request.Name.Trim();
        customer.Email = NormalizeOptional(request.Email?.ToLowerInvariant());
        customer.Phone = NormalizeOptional(request.Phone);
        customer.Address = NormalizeOptional(request.Address);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("customers.delete", cancellationToken))
        {
            return OperationResult.Forbidden("Permission customers.delete is required.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return OperationResult.NotFound("Customer not found.");
        }

        customer.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Address, customer.CreatedAt, customer.CreatedBy, customer.UpdatedAt, customer.UpdatedBy);

    private static int NormalizePage(int page) => page < DefaultPage ? DefaultPage : page;

    private static int NormalizePageSize(int pageSize) => pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

    private static int CalculateTotalPages(int totalCount, int pageSize) => totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
