using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Warehouse.Web.RazorPages.Models;

namespace Warehouse.Web.RazorPages.Pages.Tenants;

public sealed class IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<TenantResponse> Tenants { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("WarehouseApi");

        try
        {
            Tenants = await client.GetFromJsonAsync<List<TenantResponse>>(
                "api/tenants",
                cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Failed to retrieve tenants from Warehouse.Api.");

            ErrorMessage = "The tenant list could not be loaded. Check that Warehouse.Api is running.";
        }
    }
}