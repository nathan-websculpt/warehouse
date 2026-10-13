using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Warehouse.Contracts.InventoryTransferLines;

namespace Warehouse.Web.RazorPages.Pages.Transfers;

public sealed class IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<InventoryTransferLineResponse> InventoryTransferLines { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public CompleteInventoryTransferResponse? CompletionResult { get; private set; }

    public Task OnGetAsync(CancellationToken cancellationToken)
    {
        return LoadTransferLinesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostFinalizeAsync(
        [FromForm] int tenantId,
        [FromForm] Guid inventoryTransferPublicId,
        [FromForm] byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "The submitted transfer details are invalid.";
            await LoadTransferLinesAsync(cancellationToken);
            return Page();
        }

        using var client = httpClientFactory.CreateClient("WarehouseApi");

        try
        {
            using var response = await client.PostAsJsonAsync(
                $"api/transfer-lines/{tenantId}/" +
                $"{inventoryTransferPublicId}/finalize-transfer",
                new CompleteInventoryTransferRequest
                {
                    ExpectedRowVersion = expectedRowVersion
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                CompletionResult = await response.Content.ReadFromJsonAsync<CompleteInventoryTransferResponse>(cancellationToken);

                if (CompletionResult is null)
                {
                    ErrorMessage = "The API returned no completion response.";
                }
            }
            else
            {
                logger.LogWarning("Transfer {TransferId} returned HTTP {StatusCode}.", inventoryTransferPublicId, (int)response.StatusCode);

                ProblemDetails? problem = null;

                try
                {
                    problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(exception, "The API error response was not valid problem JSON.");
                }

                ErrorMessage = 
                    problem?.Detail 
                    ?? problem?.Title 
                    ?? $"The API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Could not retrieve completion confirmation for transfer {TransferId}.", inventoryTransferPublicId);
            ErrorMessage = "Could not retrieve confirmation from Warehouse.Api. Check the refreshed transfer status and API output.";
        }

        await LoadTransferLinesAsync(cancellationToken);
        return Page();
    }

    private async Task LoadTransferLinesAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("WarehouseApi");

        try
        {
            InventoryTransferLines = await client.GetFromJsonAsync<List<InventoryTransferLineResponse>>(
                "api/transfer-lines",
                cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to retrieve inventory transfers from Warehouse.Api.");
            ErrorMessage ??= "The inventory transfers list could not be loaded. Check that Warehouse.Api is running.";
        }
    }
}
