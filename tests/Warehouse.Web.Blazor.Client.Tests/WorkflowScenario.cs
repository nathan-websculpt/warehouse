using System.Net;
using System.Net.Http.Json;
using Warehouse.Contracts.Errors;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Paging;
using Warehouse.Contracts.StockAvailability;
using Warehouse.Contracts.Tenants;
using Warehouse.Contracts.Warehouses;
using Warehouse.Web.Blazor.Client.Services;

namespace Warehouse.Web.Blazor.Client.Tests;

internal sealed class WorkflowScenario : IDisposable
{
    public static readonly DateTime Created = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    public TenantResponse Tenant { get; } = new(1, Guid.NewGuid(), "Garden", true, Created, null);
    public InventoryTransferResponse Header { get; set; }
    public InventoryTransferLocationResponse Location { get; } = new(Guid.NewGuid(), "MAIN", "Main warehouse", true, 1, "PICK-01", "Pick shelf", true);
    public WarehouseResponse Warehouse { get; } = new(Guid.NewGuid(), "MAIN", "Main warehouse", true, Created, null);
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Handler { get; set; }
    public List<(HttpMethod Method, string Path)> Requests { get; } = [];
    public WarehouseApiClient Api { get; }
    private readonly HttpClient _http;

    public WorkflowScenario()
    {
        Header = new(Guid.NewGuid(), "APPROVED", 1, 2, Created, null, new byte[8], Tenant);
        
        _http = new(new CallbackHandler(async (request, token) =>
        {
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery));
            return await (Handler?.Invoke(request, token) ?? DefaultAsync(request, token));
        })) 
        {
            BaseAddress = new("https://warehouse.test/")
        };

        Api = new(_http);
    }

    public InventoryTransferDetailResponse Detail(InventoryTransferResponse? header = null) => 
        new(header ?? Header, Location, Location with { WarehouseLocationId = 2, LocationCode = "RECEIVING" },
        Enumerable.Range(1, 5).Select(id => 
            new InventoryTransferDetailLineResponse(id, id, 12.75m, Created, Guid.NewGuid(), $"SKU-{id}", "Feed", true, $"LOT-{id}", null, "L", new(100, 5, 95))).ToArray());

    public CompleteInventoryTransferResponse Receipt(bool replay = false) => 
        new(new(Header.InventoryTransferPublicId, "RECEIVED", Created, new byte[8], 1, replay),
        [new(1, "SKU-1", "LOT-1", "L", 12.75m, 1, Created)]);

    public static StockAvailabilityResponse Stock(decimal available = 15m) => 
        new(Guid.Parse("a5ab2ea3-2723-470d-932e-deb0ac975eee"), "FEED", "Feed", "L", DateOnly.FromDateTime(Created), available, available, 0, available, 0, 0, 1, "OK");

    public Task<HttpResponseMessage> DefaultAsync(HttpRequestMessage request, CancellationToken token)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (request.Method == HttpMethod.Post)
        {
            Header = Header with { TransferStatusCode = "RECEIVED" };
            return Task.FromResult(Json(Receipt()));
        }

        if (path.EndsWith("/completion"))
            return Task.FromResult(Json(Receipt(true)));

        if (path == "/api/transfers")
            return Task.FromResult(Json(new PagedResponse<InventoryTransferResponse>([Header], "next", "previous")));

        if (path.StartsWith("/api/transfers/"))
            return Task.FromResult(Json(Detail()));

        if (path.EndsWith("/stock-availability"))
            return Task.FromResult(Json(new[] { Stock() }));

        if (path.EndsWith("/warehouses"))
            return Task.FromResult(Json(new[] { Warehouse }));

        return Task.FromResult(Json(Tenant));
    }

    public static HttpResponseMessage Json<T>(T data) => new(HttpStatusCode.OK) { Content = JsonContent.Create(data) };

    public static HttpResponseMessage Problem(HttpStatusCode status, string code) => 
        new(status) { Content = JsonContent.Create(new ApiProblem(Detail: "Test rejection", Code: code, TraceId: "test-trace")) };
    
    public void Dispose() => _http.Dispose();
    
    private sealed class CallbackHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> 
            SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => 
                callback(request, cancellationToken);
    }
}