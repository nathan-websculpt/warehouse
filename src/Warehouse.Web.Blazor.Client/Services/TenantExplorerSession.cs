using Warehouse.Contracts.Tenants;
using Warehouse.Contracts.Warehouses;

namespace Warehouse.Web.Blazor.Client.Services;

public sealed class TenantExplorerSession(WarehouseApiClient api) : IDisposable
{
    private CancellationTokenSource? _request;
    private long _requestId;
    private bool _disposed;
    public Guid? TenantPublicId { get; private set; }
    public TenantResponse? Tenant { get; private set; }
    public IReadOnlyList<WarehouseResponse> Warehouses { get; private set; } = [];
    public StockReportSession Stock { get; } = new(api);
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    private async Task ReloadMetadataAsync(Guid tenantId)
    {
        _request?.Cancel();
        _request?.Dispose();
        var cancellation = _request = new CancellationTokenSource();  
        var token = cancellation.Token;
        var requestId = ++_requestId;
        IsLoading = true;
        Error = null;
        bool OwnsResponse() => !_disposed && requestId == _requestId && TenantPublicId == tenantId;

        try
        {
            var tenant = await api.GetAsync<TenantResponse>($"api/tenants/{tenantId}", token);
            var warehouses = await api.GetAsync<List<WarehouseResponse>>($"api/tenants/{tenantId}/warehouses", token);

            if (!OwnsResponse())
                return; 

            Tenant = tenant;
            Warehouses = warehouses;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) when (RequestFailures.IsExpected(exception))
        {
            if (OwnsResponse()) 
            { 
                Error = RequestFailures.Describe(exception,
                    "Related records could not be refreshed. The last successful records remain visible, if available.");
            }
        }
        finally { if (OwnsResponse()) { IsLoading = false; } }
    }

    public Task SelectTenantAsync(Guid tenantId)
    {
        if (_disposed || TenantPublicId == tenantId)
            return Task.CompletedTask;

        TenantPublicId = tenantId;
        Tenant = null;
        Warehouses = [];
        Stock.Clear();

        return ReloadMetadataAsync(tenantId);
    }

    public Task RefreshAsync() => !_disposed && TenantPublicId is { } tenantId
        ? Task.WhenAll(ReloadMetadataAsync(tenantId), Stock.RefreshAsync()) : Task.CompletedTask;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ++_requestId;

        _request?.Cancel();

        _request?.Dispose();
        Stock.Dispose();
    }
}
