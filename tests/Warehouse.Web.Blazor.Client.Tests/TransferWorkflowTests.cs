using System.Net;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Paging;
using Warehouse.Web.Blazor.Client.Services;

namespace Warehouse.Web.Blazor.Client.Tests;

public sealed class TransferWorkflowTests
{
    [Fact]
    public async Task Failed_receipt_reload_preserves_confirmed_completion_and_uses_GET()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SelectAsync(scenario.Header);
        await session.ExecuteAsync();
        
        var receipt = session.Completion;
        Assert.NotNull(receipt);
        
        scenario.Handler = (request, token) => request.RequestUri!.AbsolutePath.EndsWith("/completion")
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"))
            : scenario.DefaultAsync(request, token);
                  
        await session.ExecuteAsync();
        
        Assert.Same(receipt, session.Completion);
        Assert.Contains("remains confirmed", session.Error);
        Assert.Single(scenario.Requests, request => request.Method == HttpMethod.Post);
        Assert.Contains(scenario.Requests, request => request.Method == HttpMethod.Get && request.Path.EndsWith("/completion"));
    }

    [Fact]
    public async Task Confirmed_post_survives_detail_refresh_failure_and_disables_commands()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SelectAsync(scenario.Header);
        
        scenario.Handler = (request, token) => 
            request.Method == HttpMethod.Get 
            && request.RequestUri!.AbsolutePath.StartsWith("/api/transfers/")
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")) 
            : scenario.DefaultAsync(request, token);
        
        await session.ExecuteAsync();
        
        Assert.NotNull(session.Completion);
        Assert.False(session.IsFresh);
        Assert.False(session.CanExecute);
        Assert.True(session.ReviewRequired);
        
        scenario.Handler = null;
        
        await session.RefreshAsync();
        Assert.NotNull(session.Completion);
        Assert.True(session.IsFresh);
        
        session.Review();
        
        Assert.True(session.CanExecute);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Rejected_post_refreshes_and_requires_review(HttpStatusCode status)
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SelectAsync(scenario.Header);
        
        scenario.Handler = (request, token) => 
            request.Method == HttpMethod.Post
            ? Task.FromResult(WorkflowScenario.Problem(status, "TRANSFER_VERSION_CONFLICT"))
            : scenario.DefaultAsync(request, token);
        
        await session.ExecuteAsync();
        
        Assert.True(session.IsFresh);
        Assert.True(session.ReviewRequired);
        Assert.False(session.CanExecute);
        Assert.Equal("test-trace", session.TraceId);
        Assert.Single(scenario.Requests, request => request.Method == HttpMethod.Post);
        
        session.Review();
        
        Assert.True(session.CanExecute);
    }

    [Fact]
    public async Task Timeout_does_not_retry_and_refresh_can_discover_received_status()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        await session.SelectAsync(scenario.Header);
        
        scenario.Handler = (request, token) =>
        {
            if (request.Method != HttpMethod.Post)
                return scenario.DefaultAsync(request, token);

            scenario.Header = scenario.Header with { TransferStatusCode = "RECEIVED" };

            return Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout"));
        };
        
        await session.ExecuteAsync();
        
        Assert.Equal("RECEIVED", session.SelectedTransfer!.TransferStatusCode);
        Assert.Null(session.Completion);
        Assert.Contains("could not be confirmed", session.Error);
        Assert.True(session.ReviewRequired);
        Assert.Single(scenario.Requests, request => request.Method == HttpMethod.Post);
        
        session.Review();
        
        await session.ExecuteAsync();
        
        Assert.True(session.Completion!.Summary.IsReplay);
    }

    [Fact]
    public async Task Search_refresh_failure_does_not_prevent_detail_refresh()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SearchAsync("");
        await session.SelectAsync(scenario.Header);
        
        scenario.Header = scenario.Header with { TransferStatusCode = "RECEIVED" };
        
        scenario.Handler = (request, token) => 
            request.RequestUri!.AbsolutePath == "/api/transfers"
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"))
            : scenario.DefaultAsync(request, token);
        
        await session.RefreshAsync();
        
        Assert.True(session.IsFresh);
        Assert.Equal("RECEIVED", session.SelectedTransfer!.TransferStatusCode);
        Assert.Single(session.Results);
        Assert.Contains("last page remains", session.Error);
    }

    [Fact]
    public async Task Paging_preserves_selection_receipt_line_and_exploration_scope()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SearchAsync("SKU", 5);
        await session.SelectAsync(scenario.Header);
        
        session.SelectLine(2);
        
        await session.ExecuteAsync();
        
        var receipt = session.Completion;
        
        await session.MovePageAsync("next");
        
        Assert.Equal(2, session.SelectedLineId);
        Assert.Same(receipt, session.Completion);
        Assert.Equal(scenario.Tenant.TenantPublicId, session.RelatedTenantPublicId);
        Assert.Equal("next", session.CurrentCursor);
        
        await session.SearchAsync("Basil");
        
        Assert.Null(session.SelectedTransfer);
        Assert.Null(session.Completion);
        Assert.Equal(scenario.Tenant.TenantPublicId, session.RelatedTenantPublicId);
    }

    [Fact]
    public async Task Empty_page_falls_back_without_clearing_selected_transfer()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SelectAsync(scenario.Header);
        
        scenario.Handler = (request, token) => 
            request.RequestUri!.Query.Contains("cursor=gone")
            ? Task.FromResult(WorkflowScenario.Json(new PagedResponse<InventoryTransferResponse>([], null, null)))
            : scenario.DefaultAsync(request, token);
        
        await session.MovePageAsync("gone");
         
        Assert.Null(session.CurrentCursor);
        Assert.NotNull(session.SelectedTransfer);
        Assert.Single(session.Results);
    }

    [Fact]
    public async Task Another_transfer_clears_receipt_and_line_selection()
    {
        using var scenario = new WorkflowScenario();
        using var session = new TransferWorkflowSession(scenario.Api);
        
        await session.SelectAsync(scenario.Header);
        
        session.SelectLine(1);
        
        await session.ExecuteAsync();
        
        scenario.Header = scenario.Header with { InventoryTransferPublicId = Guid.NewGuid(), TransferStatusCode = "DRAFT" };
        
        await session.SelectAsync(scenario.Header);
        
        Assert.Null(session.Completion);
        Assert.Null(session.SelectedLineId);
        Assert.False(session.CanExecute);
    }


}

