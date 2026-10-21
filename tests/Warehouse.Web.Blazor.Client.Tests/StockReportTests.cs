using Warehouse.Contracts.StockAvailability;
using Warehouse.Web.Blazor.Client.Services;

namespace Warehouse.Web.Blazor.Client.Tests;

public sealed class StockReportTests
{
    [Fact]
    public async Task Failed_refresh_retains_report_and_successful_empty_is_distinct()
    {
        using var scenario = new WorkflowScenario();
        using var stock = new StockReportSession(scenario.Api);

        await stock.SelectAsync(new(scenario.Tenant.TenantPublicId, scenario.Warehouse.WarehousePublicId), "Main");
        
        var report = stock.Latest;
        var loadedAt = stock.LoadedAtUtc;
        
        scenario.Handler = (_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"));
        
        await stock.RefreshAsync();
        Assert.Same(report, stock.Latest);
        Assert.Equal(loadedAt, stock.LoadedAtUtc);
        Assert.True(stock.HasSuccessfulReport);
        Assert.NotNull(stock.Error);
        
        scenario.Handler = (_, _) => Task.FromResult(WorkflowScenario.Json(Array.Empty<StockAvailabilityResponse>()));
        await stock.RefreshAsync();
        Assert.True(stock.HasSuccessfulReport);
        Assert.Empty(stock.Latest);
        Assert.Null(stock.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_A_response_cannot_replace_B_or_a_new_A_request(bool returnToA)
    {
        using var scenario = new WorkflowScenario();
        using var stock = new StockReportSession(scenario.Api);
        var pending = new List<TaskCompletionSource<HttpResponseMessage>>();
        
        scenario.Handler = (_, _) =>
        {
            var source = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Add(source);
            return source.Task;
        };

        var scopeA = new StockReportScope(scenario.Tenant.TenantPublicId, Guid.NewGuid());
        var scopeB = scopeA with { WarehousePublicId = Guid.NewGuid() };
        var first = stock.SelectAsync(scopeA, "A");
        var second = stock.SelectAsync(scopeB, "B");
        var newest = returnToA ? stock.SelectAsync(scopeA, "A again") : second;
        
        pending[^1].SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock(90) }));
        await newest;
        pending[0].SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock(10) }));
        await first;
        
        if (returnToA) 
        { 
            pending[1].SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock(20) })); 
            await second; 
        }
        
        
        Assert.Equal(returnToA ? scopeA : scopeB, stock.Scope);
        Assert.Equal(90, Assert.Single(stock.Latest).AvailableToPick);
        Assert.Null(stock.Error);
        Assert.False(stock.IsLoading);
    }

    [Fact]
    public async Task Scope_change_clears_old_data_and_disposal_ignores_pending_response()
    {
        using var scenario = new WorkflowScenario();
        using var stock = new StockReportSession(scenario.Api);
        
        var scope = new StockReportScope(scenario.Tenant.TenantPublicId, scenario.Warehouse.WarehousePublicId);
        
        await stock.SelectAsync(scope, "Main");
        
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        scenario.Handler = (_, _) => pending.Task;
        var request = stock.SelectAsync(scope with { LowStockThreshold = 30 }, "Main");
        
        Assert.False(stock.HasSuccessfulReport);
        Assert.Empty(stock.Latest);
        Assert.True(stock.IsLoading);
        stock.Dispose();
        pending.SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock() }));
        
        await request;
        
        Assert.Null(stock.Scope);
        Assert.Empty(stock.Latest);
    }

    [Fact]
    public async Task Selecting_the_loading_scope_does_not_duplicate_its_request()
    {
        using var scenario = new WorkflowScenario();
        using var stock = new StockReportSession(scenario.Api);
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        scenario.Handler = (_, _) => pending.Task;
        
        var scope = new StockReportScope(Guid.NewGuid(), Guid.NewGuid());
        var first = stock.SelectAsync(scope, "Main");
        
        await stock.SelectAsync(scope, "Main");
        
        Assert.Single(scenario.Requests);
        
        pending.SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock() }));
        await first;
    }

    [Fact]
    public async Task Completion_refresh_supersedes_an_older_read_for_the_same_scope()
    {
        using var scenario = new WorkflowScenario();
        using var stock = new StockReportSession(scenario.Api);
        var pending = new List<TaskCompletionSource<HttpResponseMessage>>();
        
        scenario.Handler = (_, _) =>
        {
            var source = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Add(source);
            return source.Task;
        };
        
        var oldRead = stock.SelectAsync(new(Guid.NewGuid(), Guid.NewGuid()), "Main");
        var afterCompletion = stock.RefreshAsync();
        pending[1].SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock(70) }));
        
        await afterCompletion;
        
        pending[0].SetResult(WorkflowScenario.Json(new[] { WorkflowScenario.Stock(100) }));
        
        await oldRead;
        
        Assert.Equal(70, Assert.Single(stock.Latest).AvailableToPick);
        Assert.Null(stock.Error);
    }

}
