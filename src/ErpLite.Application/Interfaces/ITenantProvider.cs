namespace ErpLite.Application.Interfaces;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}
