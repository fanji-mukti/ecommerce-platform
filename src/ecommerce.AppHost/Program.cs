var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

// Service stubs — Aspire derives class name by replacing dots with underscores:
// ECommerce.Catalog.API.csproj → Projects.ECommerce_Catalog_API
var catalog = builder.AddProject<Projects.ECommerce_Catalog_API>("catalog")
    .WithHttpEndpoint(port: 5001)
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Cart_API>("cart")
    .WithHttpEndpoint(port: 5002)
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Checkout_API>("checkout")
    .WithHttpEndpoint(port: 5003)
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Orders_API>("orders")
    .WithHttpEndpoint(port: 5004)
    .WithReference(postgres)
    .WithReference(serviceBus);

var identity = builder.AddProject<Projects.ECommerce_Identity_API>("identity")
    .WithHttpEndpoint(port: 5005)
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithHttpEndpoint(port: 5006)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Fulfillment_API>("fulfillment")
    .WithHttpEndpoint(port: 5007)
    .WithReference(serviceBus);

var notifications = builder.AddProject<Projects.ECommerce_Notifications_API>("notifications")
    .WithHttpEndpoint(port: 5008)
    .WithReference(serviceBus);

var gateway = builder.AddProject<Projects.ECommerce_Gateway_API>("gateway")
    .WithHttpEndpoint(port: 5000)
    .WithReference(catalog)
    .WithReference(identity)
    .WithReference(notifications);

// Required for aspire publish → docker-compose.yml (prevents Pitfall 3)
builder.AddDockerComposeEnvironment("ecommerce-local");

builder.Build().Run();
