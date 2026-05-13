using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using ErpLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpLite.Api.IntegrationTests.Shared;

public static class TestDataSeeder
{
    private static readonly string[] FullPermissions =
    [
        "customers.read",
        "customers.create",
        "customers.update",
        "customers.delete",
        "products.read",
        "products.create",
        "products.update",
        "products.delete",
        "invoices.read",
        "invoices.create",
        "invoices.update",
        "invoices.delete",
        "payments.read",
        "payments.create",
        "payments.delete"
    ];

    private static readonly string[] ReadOnlyPermissions =
    [
        "customers.read",
        "products.read",
        "invoices.read",
        "payments.read"
    ];

    public static async Task<TestSeedData> SeedAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ErpLiteDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();

        if (await dbContext.Users.AnyAsync())
        {
            return await LoadExistingSeedDataAsync(dbContext);
        }

        var primaryTenant = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Primary Tenant",
            Slug = "primary-tenant"
        };

        var secondaryTenant = new Tenant
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Secondary Tenant",
            Slug = "secondary-tenant"
        };

        await dbContext.Tenants.AddRangeAsync(primaryTenant, secondaryTenant);
        await dbContext.SaveChangesAsync();

        var permissions = await dbContext.Permissions
            .ToDictionaryAsync(permission => permission.Name, StringComparer.Ordinal);

        var primaryOwnerRole = CreateRole(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            primaryTenant.Id,
            "Owner",
            FullPermissions,
            permissions);

        var primaryReaderRole = CreateRole(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
            primaryTenant.Id,
            "Reader",
            ReadOnlyPermissions,
            permissions);

        var secondaryOwnerRole = CreateRole(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            secondaryTenant.Id,
            "Owner",
            FullPermissions,
            permissions);

        var primaryOwner = CreateUser(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            primaryTenant.Id,
            "Primary Owner",
            "owner@primary.test",
            TestUsers.DefaultPassword,
            passwordHasher,
            primaryOwnerRole);

        var primaryReader = CreateUser(
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            primaryTenant.Id,
            "Primary Reader",
            "reader@primary.test",
            TestUsers.DefaultPassword,
            passwordHasher,
            primaryReaderRole);

        var secondaryOwner = CreateUser(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            secondaryTenant.Id,
            "Secondary Owner",
            "owner@secondary.test",
            TestUsers.DefaultPassword,
            passwordHasher,
            secondaryOwnerRole);

        var restrictedUser = CreateUser(
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            primaryTenant.Id,
            "Restricted User",
            "restricted@primary.test",
            TestUsers.DefaultPassword,
            passwordHasher);

        var activeCustomer = new Customer
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            TenantId = primaryTenant.Id,
            Name = "Primary Active Customer",
            Email = "customer.primary@test.local"
        };

        var deletedCustomer = new Customer
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            TenantId = primaryTenant.Id,
            Name = "Primary Deleted Customer",
            Email = "deleted.primary@test.local",
            IsDeleted = true
        };

        var crossTenantCustomer = new Customer
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            TenantId = secondaryTenant.Id,
            Name = "Secondary Customer",
            Email = "customer.secondary@test.local"
        };

        var activeProduct = new Product
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            TenantId = primaryTenant.Id,
            Name = "Primary Active Product",
            Description = "Seeded active product",
            Price = 10.50m,
            Stock = 5
        };

        var deletedProduct = new Product
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
            TenantId = primaryTenant.Id,
            Name = "Primary Deleted Product",
            Description = "Seeded deleted product",
            Price = 11.50m,
            Stock = 2,
            IsDeleted = true
        };

        var crossTenantProduct = new Product
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
            TenantId = secondaryTenant.Id,
            Name = "Secondary Product",
            Description = "Seeded secondary tenant product",
            Price = 25m,
            Stock = 9
        };

        await dbContext.Roles.AddRangeAsync(primaryOwnerRole, primaryReaderRole, secondaryOwnerRole);
        await dbContext.Users.AddRangeAsync(primaryOwner, primaryReader, secondaryOwner, restrictedUser);
        await dbContext.Customers.AddRangeAsync(activeCustomer, deletedCustomer, crossTenantCustomer);
        await dbContext.Products.AddRangeAsync(activeProduct, deletedProduct, crossTenantProduct);
        await dbContext.SaveChangesAsync();

        return new TestSeedData(
            new SeededUser(primaryOwner.Id, primaryOwner.TenantId, primaryOwner.Email, TestUsers.DefaultPassword),
            new SeededUser(primaryReader.Id, primaryReader.TenantId, primaryReader.Email, TestUsers.DefaultPassword),
            new SeededUser(secondaryOwner.Id, secondaryOwner.TenantId, secondaryOwner.Email, TestUsers.DefaultPassword),
            new SeededUser(restrictedUser.Id, restrictedUser.TenantId, restrictedUser.Email, TestUsers.DefaultPassword),
            activeCustomer.Id,
            deletedCustomer.Id,
            crossTenantCustomer.Id,
            activeProduct.Id,
            deletedProduct.Id,
            crossTenantProduct.Id);
    }

    private static Role CreateRole(
        Guid id,
        Guid tenantId,
        string name,
        IEnumerable<string> permissionNames,
        IReadOnlyDictionary<string, Permission> permissions)
    {
        var role = new Role
        {
            Id = id,
            TenantId = tenantId,
            Name = name
        };

        foreach (var permissionName in permissionNames)
        {
            role.Permissions.Add(permissions[permissionName]);
        }

        return role;
    }

    private static User CreateUser(
        Guid id,
        Guid tenantId,
        string fullName,
        string email,
        string password,
        IPasswordHasher passwordHasher,
        params Role[] roles)
    {
        var user = new User
        {
            Id = id,
            TenantId = tenantId,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHasher.Hash(password)
        };

        foreach (var role in roles)
        {
            user.Roles.Add(role);
        }

        return user;
    }

    private static async Task<TestSeedData> LoadExistingSeedDataAsync(ErpLiteDbContext dbContext)
    {
        var primaryOwner = await dbContext.Users.SingleAsync(x => x.Email == "owner@primary.test");
        var primaryReader = await dbContext.Users.SingleAsync(x => x.Email == "reader@primary.test");
        var secondaryOwner = await dbContext.Users.SingleAsync(x => x.Email == "owner@secondary.test");
        var restrictedUser = await dbContext.Users.SingleAsync(x => x.Email == "restricted@primary.test");

        return new TestSeedData(
            new SeededUser(primaryOwner.Id, primaryOwner.TenantId, primaryOwner.Email, TestUsers.DefaultPassword),
            new SeededUser(primaryReader.Id, primaryReader.TenantId, primaryReader.Email, TestUsers.DefaultPassword),
            new SeededUser(secondaryOwner.Id, secondaryOwner.TenantId, secondaryOwner.Email, TestUsers.DefaultPassword),
            new SeededUser(restrictedUser.Id, restrictedUser.TenantId, restrictedUser.Email, TestUsers.DefaultPassword),
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Guid.Parse("30000000-0000-0000-0000-000000000003"),
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            Guid.Parse("40000000-0000-0000-0000-000000000003"));
    }
}

public static class TestUsers
{
    public const string DefaultPassword = "Password123!";
}

public sealed record SeededUser(Guid UserId, Guid TenantId, string Email, string Password);

public sealed record TestSeedData(
    SeededUser PrimaryOwner,
    SeededUser PrimaryReader,
    SeededUser SecondaryOwner,
    SeededUser RestrictedUser,
    Guid ActiveCustomerId,
    Guid DeletedCustomerId,
    Guid CrossTenantCustomerId,
    Guid ActiveProductId,
    Guid DeletedProductId,
    Guid CrossTenantProductId);
