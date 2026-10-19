using System.Text.Json;

namespace Warehouse.Web.Blazor.Client.Services;

public static class RequestFailures
{
    public static bool IsExpected(Exception exception) => 
        exception is WarehouseApiException or HttpRequestException or OperationCanceledException or JsonException or NotSupportedException;
    
    public static string Describe(Exception exception, string fallback) => 
        exception is WarehouseApiException api
        ? api.Message + (api.TraceId is null ? "" : $" Reference: {api.TraceId}") 
        : fallback; 

}
