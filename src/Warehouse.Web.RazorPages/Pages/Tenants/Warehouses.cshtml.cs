using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Warehouse.Web.RazorPages.Models;

namespace Warehouse.Web.RazorPages.Pages.Tenants;

public sealed class WarehousesModel(IHttpClientFactory httpClientFactory, ILogger<WarehousesModel> logger) : PageModel
{
    public TenantResponse? Tenant { get; private set; }

    public IReadOnlyList<WarehouseResponse> Warehouses { get; private set;} = [];

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("WarehouseApi");

        try
        {
            Tenant = await client.GetFromJsonAsync<TenantResponse>($"api/tenants/{tenantPublicId}", cancellationToken);

            Warehouses =
                await client.GetFromJsonAsync<List<WarehouseResponse>>(
                    $"api/tenants/{tenantPublicId}/warehouses",
                    cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Failed to load warehouses for tenant {TenantPublicId}.",
                tenantPublicId);

            ErrorMessage = "The warehouse list could not be loaded. Check that Warehouse.Api is running.";
        }

        return Page();
    }
}