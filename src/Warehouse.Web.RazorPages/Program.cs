var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var warehouseApiBaseUrl =
    builder.Configuration["WarehouseApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "WarehouseApi:BaseUrl was not found.");

builder.Services.AddHttpClient("WarehouseApi", client =>
{
    client.BaseAddress = new Uri(warehouseApiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
