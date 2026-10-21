using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Warehouse.Api.Infrastructure;
using Warehouse.Contracts.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Api.Data;

namespace Warehouse.Api.UnitTests;

public sealed class ValidationTests
{
    [Theory]
    [InlineData(null, "20")]
    [InlineData("0", "0")]
    [InlineData("15", "15")]
    [InlineData("15.00000", "15")]
    [InlineData("15.0001", "15.0001")]
    [InlineData("999999999999999.9999", "999999999999999.9999")]
    [InlineData("00015.00000000000000000000000000000000000", "15")]
    public void Threshold_accepts_exact_values(string? text, string expected)
    {
        Assert.True(StockThreshold.TryParse(text, out var value));
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), value);

    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1000000000000000")]
    [InlineData("999999999999999.99990000000000000000000001")]
    [InlineData("15.00001")]
    [InlineData("0.00001")]
    [InlineData("1e2")]
    [InlineData("1,000")]
    [InlineData("1.2.3")]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("NaN")]
    public void Threshold_rejects_values_before_rounding(string text) =>
        Assert.False(StockThreshold.TryParse(text, out _));


    [Fact]
    public void Sql_mapping_is_specific_to_the_operation()
    {
        Assert.Equal(409, SqlErrorMapper.Map(SqlOperation.Transfer, 51003).Status);
        Assert.Equal(400, SqlErrorMapper.Map(SqlOperation.StockAvailability, 51003).Status);
        Assert.Equal(404, SqlErrorMapper.Map(SqlOperation.StockAvailability, 51004).Status);
        Assert.Equal(422, SqlErrorMapper.Map(SqlOperation.Transfer, 51004).Status);
        Assert.Equal(ErrorCodes.CompletionUnavailable, SqlErrorMapper.Map(SqlOperation.Transfer, 51015).Code);
        Assert.Equal(500, SqlErrorMapper.Map(SqlOperation.Transfer, 51012).Status);
    }

    [Fact]
    public void Sql_operation_survives_exception_middleware_clearing_the_endpoint()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new Exception(),
            Endpoint = new Endpoint(
                null,
                new EndpointMetadataCollection(new SqlOperationAttribute(SqlOperation.Transfer)),
                "transfer")
        });

        Assert.Equal(SqlOperation.Transfer, ApiProblems.GetSqlOperation(context)); 
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\r\n")]
    [InlineData("-1")]
    [InlineData("1000000000000000")]
    [InlineData("15.00001")]
    [InlineData("999999999999999.99990000000000000001")]
    public async Task Invalid_threshold_returns_structured_validation_without_SQL(string threshold)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = 
            await client.GetAsync($"/api/tenants/{Guid.NewGuid()}/warehouses/{Guid.NewGuid()}/stock-availability?lowStockThreshold={Uri.EscapeDataString(threshold)}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
        
        Assert.Equal(ErrorCodes.InvalidThreshold, problem!.Code);
        Assert.NotEmpty(problem.TraceId!);
        Assert.Contains("lowStockThreshold", problem.Errors!.Keys);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/api/transfers", "")]
    [InlineData("/api/transfers", "  ")]
    [InlineData("/api/transfers", "\t\r\n")]
    [InlineData("/api/transfer-lines", "")]
    [InlineData("/api/transfer-lines", "  ")]
    [InlineData("/api/transfer-lines", "\t\r\n")]
    public async Task Blank_cursor_returns_structured_problem_without_SQL(string path, string cursor)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync($"{path}?cursor={Uri.EscapeDataString(cursor)}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
        
        Assert.Equal(ErrorCodes.InvalidCursor, problem!.Code);
        Assert.Equal(400, problem.Status);
        Assert.NotNull(problem.Type);
        Assert.NotNull(problem.Title);
        Assert.NotNull(problem.Detail);
        Assert.NotEmpty(problem.TraceId!);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/api/transfers?cursor=invalid", "INVALID_CURSOR")]
    [InlineData("/api/transfer-lines?pageSize=0", "VALIDATION_ERROR")]
    [InlineData("/api/transfers?tenantId=nope", "VALIDATION_ERROR")]
    [InlineData("/api/does-not-exist", "NOT_FOUND")]
    public async Task Errors_have_a_consistent_contract_without_SQL(string path, string code)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync(path);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();

        Assert.Equal(code, problem!.Code);
        Assert.NotNull(problem.Type);
        Assert.NotNull(problem.Detail);
        Assert.NotEmpty(problem.TraceId!);
    }

    [Fact]
    public async Task Liveness_does_not_require_a_database()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alive")).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(9)]
    public async Task Invalid_token_length_is_a_structured_problem_before_SQL(int length)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        using var response = 
            await client.PostAsJsonAsync(
                $"/api/transfer-lines/1/{Guid.NewGuid()}/finalize-transfer",
                new { ExpectedRowVersion = new byte[length]
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
        
        Assert.Equal(ErrorCodes.Validation, problem!.Code);
        Assert.NotEmpty(problem.Errors!);
        Assert.NotEmpty(problem.TraceId!);
    }

    #region test setup
    // TestServer exercises validation/serialization only
    //      no valid database-bound request is sent
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configuration) => 
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Warehouse"] = 
                    "Server=not-used.invalid;Database=NotUsed;Integrated Security=True;Connect Timeout=1"
            }));

            builder.ConfigureServices(
                services => services.AddDbContext<WarehouseDbContext>(
                    options => options.AddInterceptors(new RejectDatabaseAccess())));
        });

    private sealed class RejectDatabaseAccess : DbConnectionInterceptor
    {
        // EF Core calls these overrides automatically before opening a database connection...
        //      both sync and async paths are guarded. Passing validation tests should never
        //      reach either method -- any attempted EF database connection throws and fails the test

        public override InterceptionResult 
            ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
                throw new InvalidOperationException("Validation tests must reject the request before database access.");

        public override ValueTask<InterceptionResult> 
            ConnectionOpeningAsync(
                DbConnection connection,
                ConnectionEventData eventData,
                InterceptionResult result,
                CancellationToken cancellationToken = default) =>
                    throw new InvalidOperationException("Validation tests must reject the request before database access.");
    }
    #endregion
}
