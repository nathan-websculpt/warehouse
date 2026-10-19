using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Warehouse.Web.Blazor.Client.Layout;

public sealed class WorkflowErrorBoundary : ErrorBoundary
{
    [Inject]
    public ILogger<WorkflowErrorBoundary> Logger { get; set; } = null!;

    public string? Reference { get; private set; }

    protected override Task OnErrorAsync(Exception exception)
    {
        Reference = Guid.NewGuid().ToString("N");
        Logger.LogError(exception, "Rendering failed. Reference: {Reference}.", Reference);

        return Task.CompletedTask;  
    }
}
