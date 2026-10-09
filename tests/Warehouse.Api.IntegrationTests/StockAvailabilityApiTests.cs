using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Warehouse.Contracts.StockAvailability;
using Warehouse.Api.Data;

namespace Warehouse.Api.IntegrationTests;

public sealed class StockAvailabilityApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StockAvailabilityApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStockAvailability_ForCedarMain_ReturnsReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        var cedar = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantName == "Cedar Pet Supplies")
            .Select(tenant => new
            {
                tenant.TenantId,
                tenant.TenantPublicId
            }).SingleAsync();

        var cedarMainWarehousePublicId =
            await dbContext.Warehouses
                .AsNoTracking()
                .Where(warehouse =>
                    warehouse.TenantId == cedar.TenantId &&
                    warehouse.WarehouseName == "Cedar main warehouse")
                .Select(warehouse => warehouse.WarehousePublicId)
                .SingleAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/tenants/{cedar.TenantPublicId}" +
            $"/warehouses/{cedarMainWarehousePublicId}" +
            "/stock-availability?lowStockThreshold=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<List<StockAvailabilityResponse>>();

        Assert.NotNull(report);
        Assert.Equal(4, report.Count);

        var harness = Assert.Single(report, item => item.SKU == "P2-DOG-HARNESS");

        Assert.Equal(0m, harness.PhysicalOnHand);
        Assert.Equal(0m, harness.AvailableToPick);
        Assert.Equal(0, harness.EligibleLotCount);
        Assert.Equal("OUT_OF_STOCK", harness.StockStatus);

        foreach (var item in report)
        {
            Assert.Equal(
                item.PhysicalOnHand,
                item.EligibleOnHand +
                item.ExpiredOnHand +
                item.InactiveLocationOnHand);

            Assert.Equal(
                item.AvailableToPick,
                item.EligibleOnHand -
                item.EligibleReserved);

            Assert.True(item.AvailableToPick >= 0);
        }
    }
}