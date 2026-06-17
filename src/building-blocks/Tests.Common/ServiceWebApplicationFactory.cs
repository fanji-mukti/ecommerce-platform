using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Tests.Common;

/// <summary>
/// Generic WebApplicationFactory base class for integration tests.
/// Swaps the "postgres" connection string to use the Testcontainers instance.
/// </summary>
public class ServiceWebApplicationFactory<TProgram>(string connectionString)
    : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:postgres"] = connectionString
            });
        });
    }
}
