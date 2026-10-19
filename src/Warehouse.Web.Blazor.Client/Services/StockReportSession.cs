using Warehouse.Contracts.StockAvailability;

namespace Warehouse.Web.Blazor.Client.Services;

public sealed record StockReportScope(Guid TenantPublicId, Guid WarehousePublicId, decimal LowStockThreshold = 20m);

/// <summary>Retains the latest observation for one scope.</summary>
public sealed class StockReportSession(WarehouseApiClient api) : IDisposable
{
    private CancellationTokenSource? _request;
    private long _requestId;
    private bool _disposed;
    public StockReportScope? Scope { get; private set; }
    public string? WarehouseName { get; private set; }
    public IReadOnlyList<StockAvailabilityResponse> Latest { get; private set; } = [];
    public bool HasSuccessfulReport { get; private set; }
    public DateTimeOffset? LoadedAtUtc { get; private set; }
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    private async Task LoadAsync(StockReportScope scope)
    {
        _request?.Cancel();
        _request?.Dispose();
        var cancellation = _request = new CancellationTokenSource();
        var token = cancellation.Token;
        var requestId = ++_requestId;
        IsLoading = true;
        Error = null; 
        bool OwnsResponse() => !_disposed && requestId == _requestId && Scope == scope;

        try
        {
            var report = await api.GetStockAsync(scope, token);

            if (!OwnsResponse())
                return;

            Latest = report.ToArray();
            HasSuccessfulReport = true;
            LoadedAtUtc = DateTimeOffset.UtcNow;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) when (RequestFailures.IsExpected(exception))
        {
            if (OwnsResponse()) 
            { 
                Error = "Stock report refresh failed. " + 
                    RequestFailures.Describe(exception, "The report could not be loaded.") + 
                    " The latest successful report remains visible, if available.";
            }
        }
        finally { if (OwnsResponse()) { IsLoading = false; } }
    }

    public Task SelectAsync(StockReportScope scope, string warehouseName)
    {
        if (_disposed || (Scope == scope && IsLoading))
            return Task.CompletedTask;

        if (Scope != scope)
        {
            Latest = [];
            HasSuccessfulReport = false;
            LoadedAtUtc = null;
        }

        Scope = scope;
        WarehouseName = warehouseName;
        
        return LoadAsync(scope);
    }

    // completion-triggered refresh must supersede a read started before posting
    //      manual refresh controls are disabled while loading; programmatic overlap accepts only the newest request...
    public Task RefreshAsync() => !_disposed && Scope is { } scope ? LoadAsync(scope) : Task.CompletedTask;

    public void Clear()
    {
        ++_requestId;
        _request?.Cancel();
        _request?.Dispose();
        _request = null;
        Scope = null;
        WarehouseName = null;
        Latest = [];
        HasSuccessfulReport = false;
        LoadedAtUtc = null;
        IsLoading = false;
        Error = null;
    }

    public void Dispose() 
    { 
        if (_disposed)
            return;

        _disposed = true;
        Clear();
    }
}

