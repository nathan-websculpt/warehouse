using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.InventoryTransfers;

/*
 * STUDY NOTES:
 * A 'service' in Blazor provides reusable functionality to components.
 *      Components handle things such as buttons, tables, and loading messages.
 *      A service can handle work such as API communication or shared application state.
 *      
 *  'WarehouseApiClient' is the Blazor client's shared code for communicating with the Warehouse API.
 *      It sends HTTP requests, converts JSON responses in C# objects, and gives components a way to handle API errors.
 *      It runs in the browser alongside the components.
*/

namespace Warehouse.Web.Blazor.Client.Services;

public sealed class WarehouseApiClient(HttpClient http)
{
    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) where T : class
    {
        using var response = await http.GetAsync(path, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    public async Task<CompleteInventoryTransferResponse> FinalizeAsync(InventoryTransferResponse transfer, CancellationToken cancellationToken = default)
    {
        var path = $"api/transfer-lines/{transfer.Tenant.TenantId}/{transfer.InventoryTransferPublicId}/finalize-transfer";

        var request = new CompleteInventoryTransferRequest
        {
            ExpectedRowVersion = transfer.RowVersion.ToArray()
        };

        using var response = await http.PostAsJsonAsync(path, request, cancellationToken);
        return await ReadAsync<CompleteInventoryTransferResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new JsonException("The API returned an empty response.");
        }

        ApiProblem? problem = null;

        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) 
            when (exception is JsonException or NotSupportedException)
        {
            // TODO:
            // a proxy or failed server can return a non-JSON error body
        }

        var validation =
            problem?.Errors is { Count: > 0 } errors
                ? string.Join(
                    " ",
                    errors.Values.SelectMany(values => values))
                : null;

        throw new WarehouseApiException(
            response.StatusCode,
            validation
                ?? problem?.Detail
                ?? problem?.Title
                ?? $"The API returned HTTP {(int)response.StatusCode}.", problem?.TraceId);
    }
}

public sealed record ApiProblem(
    string? Title,
    string? Detail,
    string? TraceId,
    Dictionary<string, string[]>? Errors);

public sealed class WarehouseApiException(HttpStatusCode statusCode, string message, string? traceId) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? TraceId { get; } = traceId;
}