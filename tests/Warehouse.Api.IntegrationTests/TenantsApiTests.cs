using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Warehouse.Api.Contracts.Tenants;
using Warehouse.Api.Contracts.Warehouses;
using Warehouse.Api.Data;

namespace Warehouse.Api.IntegrationTests;

public sealed class TenantsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TenantsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenants_ReturnsTenants()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tenants = await response.Content.ReadFromJsonAsync<List<TenantResponse>>();

        Assert.NotNull(tenants);
        Assert.NotEmpty(tenants);
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var tenantPublicId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{tenantPublicId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_WhenTenantExists_ReturnsTenant()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        var expectedTenant = await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.TenantId)
            .Select(tenant => new TenantResponse(
                tenant.TenantPublicId,
                tenant.TenantName,
                tenant.IsActive,
                tenant.CreatedAtUtc,
                tenant.UpdatedAtUtc))
            .FirstAsync();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{expectedTenant.TenantPublicId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualTenant = await response.Content.ReadFromJsonAsync<TenantResponse>();

        Assert.NotNull(actualTenant);
        Assert.Equal(expectedTenant, actualTenant);
    }

    [Fact]
    public async Task GetWarehouses_WhenTenantExists_ReturnsTenantsWarehouses()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        var tenantPublicId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Warehouses.Any())
            .Select(tenant => tenant.TenantPublicId)
            .FirstAsync();

        var expectedWarehouses = await dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.Tenant.TenantPublicId == tenantPublicId)
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Select(warehouse => new WarehouseResponse(
                warehouse.WarehousePublicId,
                warehouse.WarehouseCode,
                warehouse.WarehouseName,
                warehouse.IsActive,
                warehouse.CreatedAtUtc,
                warehouse.UpdatedAtUtc))
            .ToListAsync();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{tenantPublicId}/warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualWarehouses = await response.Content.ReadFromJsonAsync<List<WarehouseResponse>>();

        Assert.NotNull(actualWarehouses);

        Assert.Equal(expectedWarehouses, actualWarehouses);
    }

    [Fact]
    public async Task GetWarehouses_WhenTenantDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var tenantPublicId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{tenantPublicId}/warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

