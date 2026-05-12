using ErpLite.Application.Interfaces;
using ErpLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Tests.Shared;

public static class InMemoryDbContextFactory
{
    public static ErpLiteDbContext Create(
        string? databaseName = null,
        Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<ErpLiteDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new ErpLiteDbContext(options, new TestCurrentUserService(userId));
    }

    private sealed class TestCurrentUserService(Guid? userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
    }
}
