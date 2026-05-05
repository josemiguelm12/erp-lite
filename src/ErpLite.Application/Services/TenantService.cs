using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class TenantService(IRepository<Tenant> tenants, IUnitOfWork unitOfWork) : ITenantService
{
    public async Task<Result<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant();
        if (slug is not null && await tenants.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken) is not null)
        {
            return Result<TenantResponse>.Failure("Tenant slug already exists.");
        }

        var tenant = new Tenant { Name = request.Name.Trim(), Slug = slug };
        await tenants.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantResponse>.Success(new TenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt));
    }
}
