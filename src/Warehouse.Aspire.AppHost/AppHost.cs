var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.Warehouse_Api>(
        "warehouse-api",
        launchProfileName: "https")
    .WithHttpHealthCheck(
        "/health",
        endpointName: "https");

builder
    .AddProject<Projects.Warehouse_Web_Blazor>(
        "warehouse-web",
        launchProfileName: "https")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpHealthCheck(
        "/health",
        endpointName: "https");

builder
    .AddProject<Projects.Warehouse_Web_RazorPages>(
        "warehouse-razor",
        launchProfileName: "https")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpHealthCheck(
        "/health",
        endpointName: "https")
    .WithExplicitStart();

builder.Build().Run();