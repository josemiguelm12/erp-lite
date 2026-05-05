using ErpLite.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ErpLite.Infrastructure.Tenancy;

public sealed class HttpTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public Guid? TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst("tenantId")?.Value;
            return Guid.TryParse(value, out var tenantId) ? tenantId : null;
        }
    }
}
