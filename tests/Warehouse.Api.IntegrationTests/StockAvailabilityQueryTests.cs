using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;

namespace Warehouse.Api.IntegrationTests;

public sealed class StockAvailabilityQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StockAvailabilityQueryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteAsync_WhenWarehouseBelongsToDifferentTenant_ThrowsSqlException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        var stockReadinessQuery = scope.ServiceProvider.GetRequiredService<StockAvailabilityQuery>();

        var cedarTenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantName == "Cedar Pet Supplies")
            .Select(tenant => tenant.TenantId)
            .SingleAsync();

        var mapleTenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantName == "Maple Pet Supplies")
            .Select(tenant => tenant.TenantId)
            .SingleAsync();

        var mapleWarehousePublicId =
            await dbContext.Warehouses
                .AsNoTracking()
                .Where(warehouse => warehouse.TenantId == mapleTenantId)
                .Select(warehouse => warehouse.WarehousePublicId)
                .SingleAsync();

        // Act
        var exception =
            await Assert.ThrowsAsync<SqlException>(
                () => stockReadinessQuery.ExecuteAsync(
                    cedarTenantId,
                    mapleWarehousePublicId,
                    20.0000m,
                    CancellationToken.None));

        // Assert
        Assert.Equal(51006, exception.Number);

        Assert.Contains("Warehouse not found for this tenant.", exception.Message);
    }
}