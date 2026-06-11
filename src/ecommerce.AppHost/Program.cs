var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

// Service stubs — Aspire derives class name by replacing dots with underscores:
// ECommerce.Catalog.API.csproj → Projects.ECommerce_Catalog_API
builder.AddProject<Projects.ECommerce_Catalog_API>("catalog")
    .WithEndpoint(
        name: "http",
        port: 5001,
        targetPort: 5001,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Cart_API>("cart")
    .WithEndpoint(
        name: "http",
        port: 5002,
        targetPort: 5002,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Checkout_API>("checkout")
    .WithEndpoint(
        name: "http",
        port: 5003,
        targetPort: 5003,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Orders_API>("orders")
    .WithEndpoint(
        name: "http",
        port: 5004,
        targetPort: 5004,
        scheme: "http",
        isExternal: true)
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Identity_API>("identity")
    .WithEndpoint(
        name: "http",
        port: 5005,
        targetPort: 5005,
        scheme: "http",
        isExternal: true)
    .WithReference(postgres);

builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithEndpoint(
        name: "http",
        port: 5006,
        targetPort: 5006,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 5006, name: "http")
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Fulfillment_API>("fulfillment")
    .WithEndpoint(
        name: "http",
        port: 5007,
        targetPort: 5007,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Notifications_API>("notifications")
    .WithEndpoint(
        name: "http",
        port: 5008,
        targetPort: 5008,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(serviceBus);

// Required for aspire publish → docker-compose.yml (prevents Pitfall 3)
builder.AddDockerComposeEnvironment("ecommerce-local");

builder.Build().Run();
