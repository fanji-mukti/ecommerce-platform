var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

// Service stubs — Aspire derives class name by replacing dots with underscores:
// ECommerce.Catalog.API.csproj → Projects.ECommerce_Catalog_API
builder.AddProject<Projects.ECommerce_Catalog_API>("catalog")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Cart_API>("cart")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Checkout_API>("checkout")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Orders_API>("orders")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Identity_API>("identity")
    .WithReference(postgres);

builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Fulfillment_API>("fulfillment")
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Notifications_API>("notifications")
    .WithReference(serviceBus);

// Required for aspire publish → docker-compose.yml (prevents Pitfall 3)
builder.AddDockerComposeEnvironment("ecommerce-local");

builder.Build().Run();
