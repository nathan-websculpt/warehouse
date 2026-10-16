using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); // comes from existing reference to Warehouse.Aspire.ServiceDefaults

// gives the client structured error information and a trace reference
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;
    };
});

var connectionString =
    builder.Configuration.GetConnectionString("Warehouse")
    ?? throw new InvalidOperationException(
        "Connection string 'Warehouse' was not found.");

builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));

builder.Services.AddScoped<StockAvailabilityQuery>();
builder.Services.AddScoped<CompleteInventoryTransferQuery>();

builder.Services.AddControllers();

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