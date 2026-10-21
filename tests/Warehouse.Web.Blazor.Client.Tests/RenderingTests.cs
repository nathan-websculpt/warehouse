using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Web.Blazor.Client.Layout;
using Warehouse.Web.Blazor.Client.Pages;

namespace Warehouse.Web.Blazor.Client.Tests;

public sealed class RenderingTests
{
    [Fact]
    public async Task Empty_transfer_remains_inspectable_after_refresh()
    {
        using var scenario = new WorkflowScenario();

        scenario.Handler = (request, token) => 
            request.RequestUri!.AbsolutePath.StartsWith("/api/transfers/")
                ? Task.FromResult(WorkflowScenario.Json(scenario.Detail() with { Lines = [] })) 
                : scenario.DefaultAsync(request, token);

        await using var context = new BunitContext();

        context.Services.AddSingleton(scenario.Api);

        var cut = context.Render<Transfers>();
        cut.FindAll("button").Single(button => button.TextContent == "Inspect").Click();
        cut.WaitForAssertion(() => Assert.Contains("This transfer has no lines.", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent == "Refresh").Click();  

        cut.WaitForAssertion(() => Assert.Contains($"Transfer {scenario.Header.InventoryTransferPublicId}", cut.Markup));

        Assert.Contains("This transfer has no lines.", cut.Markup);
    }

    [Fact]
    public async Task Line_selection_and_related_stock_survive_transfer_refresh_failure()
    {
        using var scenario = new WorkflowScenario();
        await using var context = new BunitContext();

        context.Services.AddSingleton(scenario.Api);

        var cut = context.Render<Transfers>();
        cut.FindAll("button").Single(button => button.TextContent == "Inspect").Click();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("table.align-middle tbody tr").Count));
        cut.Find("button[aria-label='Select SKU-2, lot LOT-2']").Click();
        var explorer = cut.FindComponent<TenantExplorer>();
        explorer.FindAll("button").Single(button => button.TextContent.Contains("MAIN")).Click();

        explorer.WaitForAssertion(() => Assert.Contains("FEED", explorer.Markup));
        
        scenario.Handler = (request, token) => request.RequestUri!.AbsolutePath.EndsWith("/stock-availability")
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")) 
            : scenario.DefaultAsync(request, token);
        
        cut.FindAll("button").Single(button => button.TextContent == "Refresh").Click();
        
        cut.WaitForAssertion(() => 
            Assert.Equal("true", cut.Find("button[aria-label='Select SKU-2, lot LOT-2']").GetAttribute("aria-pressed")));
        
        Assert.Same(explorer.Instance, cut.FindComponent<TenantExplorer>().Instance);
        explorer.WaitForAssertion(() => Assert.NotEmpty(explorer.FindAll("[role=alert]")));
        Assert.Contains("FEED", explorer.Markup);
    }

    [Fact]
    public async Task Rendering_exception_has_a_safe_reload_fallback()
    {
        await using var context = new BunitContext();

        RenderFragment body = builder => { builder.OpenComponent<ThrowingComponent>(0); builder.CloseComponent(); };
        
        var cut = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, body));
        cut.WaitForAssertion(() => Assert.Contains("This view could not be displayed.", cut.Markup));
        
        Assert.DoesNotContain("private exception details", cut.Markup);
        Assert.NotNull(cut.FindComponent<WorkflowErrorBoundary>().Instance.Reference);
        
        cut.Find("[role=alert] button").Click();
    }


    public sealed class ThrowingComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => 
            throw new InvalidOperationException("private exception details");
    }
}