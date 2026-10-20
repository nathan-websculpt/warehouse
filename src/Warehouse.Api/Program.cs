using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;
using Warehouse.Api.Infrastructure;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// register redaction before exporters so no exported span contains query text
builder.Services.AddOpenTelemetry().
    WithTracing(tracing => 
        tracing.AddProcessor(new SqlTelemetryRedactor()));

builder.AddServiceDefaults(); // comes from existing reference to Warehouse.Aspire.ServiceDefaults

// extend the existing provider once -- SqlClient also covers EF's commands
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
    .AddSqlClientInstrumentation(options =>
    {
        options.RecordException = false;
    }));

// gives the client structured error information and a trace reference
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context => ApiProblems.Normalize(context.ProblemDetails, context.HttpContext);
});

builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var connectionString =
    builder.Configuration.GetConnectionString("Warehouse")
    ?? throw new InvalidOperationException(
        "Connection string 'Warehouse' was not found.");

builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));

builder.Services.AddHealthChecks()
    .AddCheck<WarehouseReadiness>(
    "warehouse-database",
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(5));

builder.Services.AddScoped<StockAvailabilityQuery>();
builder.Services.AddScoped<CompleteInventoryTransferQuery>();

builder.Services.AddControllers(options => options.Filters.Add<ApiProblemFilter>());

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints(); // comes from existing reference to Warehouse.Aspire.ServiceDefaults

app.Run();

public partial class Program { }
