using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Warehouse")
    ?? throw new InvalidOperationException(
        "Connection string 'Warehouse' was not found.");

builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));

builder.Services.AddScoped<StockAvailabilityQuery>();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }