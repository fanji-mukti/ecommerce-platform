using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.API.Data;

public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
