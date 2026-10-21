using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Warehouse.Contracts.Errors;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.Paging;

namespace Warehouse.Web.RazorPages.Pages.Transfers;

public sealed class IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Cursor { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public string? NextCursor { get; private set; }
    public string? PreviousCursor { get; private set; }

    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<InventoryTransferLineResponse> InventoryTransferLines { get; private set; } = [];
    public CompleteInventoryTransferResponse? CompletionResult { get; private set; }

    public Task OnGetAsync(CancellationToken cancellationToken) => 
        LoadTransferLinesAsync(cancellationToken);

     
    public async Task OnGetCompletionAsync(int tenantId, Guid inventoryTransferPublicId, CancellationToken cancellationToken)
    {
        await ReadCompletionAsync(tenantId, inventoryTransferPublicId, null, cancellationToken);
        await LoadTransferLinesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostFinalizeAsync(
        [FromForm] int tenantId,
        [FromForm] Guid inventoryTransferPublicId,
        [FromForm] byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || expectedRowVersion is not { Length: 8 })
            ErrorMessage = "The submitted transfer details are invalid.";
        else
            await ReadCompletionAsync(tenantId, inventoryTransferPublicId, expectedRowVersion, cancellationToken);

        await LoadTransferLinesAsync(cancellationToken);
        return Page();
    }

    private async Task ReadCompletionAsync(int tenantId, Guid publicId, byte[]? version, CancellationToken token)
    {
        using var client = httpClientFactory.CreateClient("WarehouseApi");
        try
        {
            using var response = version is null
                ? await client.GetAsync($"api/transfers/{tenantId}/{publicId}/completion", token)
                : await client.PostAsJsonAsync($"api/transfer-lines/{tenantId}/{publicId}/finalize-transfer",
                    new CompleteInventoryTransferRequest { ExpectedRowVersion = version }, token);

            if (response.IsSuccessStatusCode)
            {
                CompletionResult = await response.Content.ReadFromJsonAsync<CompleteInventoryTransferResponse>(token);

                if (CompletionResult is null)
                    ErrorMessage = "The API returned no completion response.";
            }
            else 
            {
                ErrorMessage = await DescribeAsync(response, token);
            }
        }
        catch (Exception exception) when (IsRequestFailure(exception))
        {
            if (token.IsCancellationRequested)
                throw;

            logger.LogWarning(exception, "Could not retrieve confirmation for {TransferId}.", publicId);

            ErrorMessage = version is null
                ? "Completion details could not be retrieved."
                : "Completion could not be confirmed. Inspect the refreshed transfer before retrying.";
        }
    }

    private async Task LoadTransferLinesAsync(CancellationToken token)
    {
        using var client = httpClientFactory.CreateClient("WarehouseApi");
        try
        {
            async Task<PagedResponse<InventoryTransferLineResponse>?> FetchAsync()
            {
                var path = $"api/transfer-lines?q={Uri.EscapeDataString(Q?.Trim() ?? "")}&pageSize={PageSize}" + (Cursor is null ? "" : $"&cursor={Uri.EscapeDataString(Cursor)}");
                using var response = await client.GetAsync(path, token);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage ??= await DescribeAsync(response, token);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<PagedResponse<InventoryTransferLineResponse>>(token)
                    ?? throw new JsonException("Missing page.");
            }

              
            var page = await FetchAsync();

            if (page?.Items.Count == 0 && Cursor is not null)
            {
                Cursor = null;
                page = await FetchAsync();
            }

            if (page is null)
                return;

            InventoryTransferLines = page.Items;
            NextCursor = page.NextCursor;
            PreviousCursor = page.PreviousCursor;
        }
        catch (Exception exception)
            when (IsRequestFailure(exception))
        {
            if (token.IsCancellationRequested)
                throw;

            logger.LogWarning(exception, "Failed to retrieve transfer lines.");
            ErrorMessage ??= "The transfer list could not be loaded. Any confirmed completion above remains confirmed.";
        }
    }

    private static async Task<string> DescribeAsync(HttpResponseMessage response, CancellationToken token)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(token);
            
            if (problem is not null)
            {
                var detail = problem.Errors is { Count: > 0 } errors
                    ? string.Join(" ", errors.Values.SelectMany(values => values))
                    : problem.Detail ?? problem.Title;

                return $"{detail ?? "The request was rejected."} Code: {problem.Code ?? "unavailable"}. Reference: {problem.TraceId ?? "unavailable"}.";
            }
        }
        catch (Exception exception)
            when (exception is JsonException or NotSupportedException)
        {
            // TODO:
        }

        return $"The API returned HTTP {(int)response.StatusCode}.";
    }


    private static bool IsRequestFailure(Exception exception) =>
        exception is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException;
} 