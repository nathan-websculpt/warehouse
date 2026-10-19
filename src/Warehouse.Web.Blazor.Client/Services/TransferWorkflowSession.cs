using System.Net;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Paging;

namespace Warehouse.Web.Blazor.Client.Services;

/// <summary>Owns one screen's selection, command knowledge, and paging. Never retries a POST.</summary>
public sealed class TransferWorkflowSession(WarehouseApiClient api) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;
    public IReadOnlyList<InventoryTransferResponse> Results { get; private set; } = [];
    public InventoryTransferResponse? SelectedTransfer { get; private set; }
    public InventoryTransferDetailResponse? Detail { get; private set; } 
    public int? SelectedLineId { get; private set; }
    public CompleteInventoryTransferResponse? Completion { get; private set; }
    public Guid? RelatedTenantPublicId { get; private set; }
    public int RelatedRefreshVersion { get; private set; }
    public bool IsBusy { get; private set; } 
    public bool IsFresh { get; private set; }
    public bool ReviewRequired { get; private set; }
    public bool SearchLoaded { get; private set; }
    public string? Error { get; private set; }
    public string? TraceId { get; private set; }
    public string AppliedQuery { get; private set; } = "";
    public int PageSize { get; private set; } = 25;
    public string? CurrentCursor { get; private set; }
    public string? NextCursor { get; private set; }
    public string? PreviousCursor { get; private set; }
    public bool CanExecute => !IsBusy && IsFresh && !ReviewRequired && SelectedTransfer?.TransferStatusCode is "APPROVED" or "RECEIVED";

    #region API Work

    // when someone calls SearchAsync, give RunAsync this asynchronous work to perform, and return its Task
    public Task SearchAsync(string query, int pageSize = 25) => RunAsync(async token =>
    {
        query = query.Trim();
        var page = await api.SearchTransfersAsync(query, pageSize, null, token);
        token.ThrowIfCancellationRequested();
        AppliedQuery = query;
        PageSize = pageSize;
        SetPage(page, null);
        SelectedTransfer = null;
        Detail = null;
        SelectedLineId = null;
        Completion = null;
        IsFresh = false;
        ReviewRequired = false;
        // related records have their own tenant/warehouse scope and lifetime
    });

    // called from FinalizeAsync() and tests
    public Task ExecuteAsync()
    {
        if (!CanExecute || SelectedTransfer is not { } selected) { return Task.CompletedTask; }
        return RunAsync(async token =>
        {
            // replay a transfer - view movements
            if (selected.TransferStatusCode == "RECEIVED")
            {
                try
                {
                    var receipt = await api.RetrieveCompletionAsync(selected, token);
                    token.ThrowIfCancellationRequested();
                    Completion = receipt;
                }
                catch (Exception exception) when (RequestFailures.IsExpected(exception))
                {
                    token.ThrowIfCancellationRequested(); 
                    RecordError(exception, Completion is null
                        ? "Completion details could not be retrieved."
                        : "Completion remains confirmed; its details could not be retrieved again.");
                }
                return;
            }

            IsFresh = false;
            ReviewRequired = true;
             
            try
            {
                var receipt = await api.FinalizeAsync(selected, token);
                token.ThrowIfCancellationRequested();
                Completion = receipt;
                ReviewRequired = false;
            }
            catch (Exception exception) when (RequestFailures.IsExpected(exception))
            {
                token.ThrowIfCancellationRequested();
                // the => arrows here are switch-arm separators... “for this matching case, use this result”
                //      each comma-separated entry is called an arm
                //      _ is a discard pattern... it matches any value without introducing a variable name. Here it acts as “match any status"
                var guidance = exception is WarehouseApiException apiError
                    ? apiError.StatusCode switch
                    {
                        HttpStatusCode.Conflict => "The transfer state or version changed. Review the refreshed transfer.",
                        _ when (int)apiError.StatusCode < 500 => "Finalization was rejected. Review the refreshed transfer.",
                        _ => "Completion could not be confirmed. Check the refreshed state before retrying."
                    }
                    : "Completion could not be confirmed. The request may have completed. Check the refreshed state before retrying.";
                RecordError(exception, guidance);
            }
            await RefreshDataAsync(token);
        });
    }
      
    private async Task LoadPageAsync(string? cursor, CancellationToken token)
    {
        var page = await api.SearchTransfersAsync(AppliedQuery, PageSize, cursor, token);

        if (page.Items.Count == 0 && cursor is not null)
        {
            cursor = null;
            page = await api.SearchTransfersAsync(AppliedQuery, PageSize, null, token);
        }

        token.ThrowIfCancellationRequested();
        SetPage(page, cursor);
    }

    private async Task LoadSelectionAsync(InventoryTransferResponse transfer, CancellationToken token)
    {
        var detail = await api.GetTransferAsync(transfer, token);
        token.ThrowIfCancellationRequested();
        Detail = detail;
        SelectedTransfer = detail.Transfer;

        if (!detail.Lines.Any(line => line.InventoryTransferLineId == SelectedLineId))
            SelectedLineId = null;

        IsFresh = true;
    }

    #endregion

    #region API Work (through other methods)

    public Task MovePageAsync(string? cursor) => RunAsync(token => LoadPageAsync(cursor, token));

    public Task SelectAsync(InventoryTransferResponse transfer) => RunAsync(async token =>
    {
        bool sameTransfer = SelectedTransfer is { } selected &&
            selected.Tenant.TenantId == transfer.Tenant.TenantId &&
            selected.InventoryTransferPublicId == transfer.InventoryTransferPublicId;

        if (!sameTransfer)
        {
            Detail = null;
            SelectedLineId = null;
            Completion = null;  
        }

        SelectedTransfer = transfer;
        RelatedTenantPublicId = transfer.Tenant.TenantPublicId;
        IsFresh = false;
        ReviewRequired = false; 

        await LoadSelectionAsync(transfer, token);
    });

    public Task RefreshAsync() => RunAsync(RefreshDataAsync);

    private async Task RefreshDataAsync(CancellationToken token)
    {
        if (SelectedTransfer is { } selected)
        {
            IsFresh = false;

            try { 
                await LoadSelectionAsync(selected, token);
            }
            catch (Exception exception) when (RequestFailures.IsExpected(exception))
            {
                token.ThrowIfCancellationRequested();
                ReviewRequired = true;
                RecordError(exception, 
                    (Completion is null ? "Transfer reload failed." : "Completion remains confirmed; transfer reload failed.") + " Refresh before another command.");
            }
        }

        // search and selected detail are independent reads... failure in one cannot erase the other
        try { 
            await LoadPageAsync(CurrentCursor, token); 
        }
        catch (Exception exception) when (RequestFailures.IsExpected(exception))
        {
            token.ThrowIfCancellationRequested(); 
            RecordError(exception, "Search results could not be refreshed; the last page remains visible.");
        }

        RelatedRefreshVersion++;
    }

    #endregion

    #region Update Local State

    public void SelectLine(int lineId)
    {
        if (!IsBusy && Detail?.Lines.Any(line => line.InventoryTransferLineId == lineId) == true)
            SelectedLineId = lineId;
    }

    public void Review() 
    {
        if (!IsBusy && IsFresh) 
            ReviewRequired = false;
    }

    private void SetPage(PagedResponse<InventoryTransferResponse> page, string? cursor)
    {
        Results = page.Items;
        NextCursor = page.NextCursor;
        PreviousCursor = page.PreviousCursor;
        CurrentCursor = cursor;
        SearchLoaded = true;
    }

    #endregion

    #region Execution, Error Handling, and Lifetime Management

    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy || _disposed)
            return;

        IsBusy = true;
        Error = null;
        TraceId = null;

        try { await action(_lifetime.Token); }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception exception) when (RequestFailures.IsExpected(exception)) 
        { 
            RecordError(exception, "The data could not be loaded. Try refreshing.");   
        }
        finally { IsBusy = false; }
    }

    private void RecordError(Exception exception, string guidance)
    {
        var message = exception is WarehouseApiException apiError ? $"{guidance} {apiError.Message}" : guidance;
         
        Error = Error is null ? message : $"{Error} {message}";

        if (exception is WarehouseApiException api)
            TraceId = api.TraceId ?? TraceId;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    #endregion
}

