using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Warehouse.Web.Blazor.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// _ is here because the service provider is currently unused
// creating the HttpClient only requires values available through 'builder'
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(30)
});

builder.Services.AddScoped<WarehouseApiClient>();

await builder.Build().RunAsync();