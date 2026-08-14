using ECommerce.Catalog.API.Features.Products;
using ECommerce.Catalog.Events.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.API.Data;

public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync(ct);

        if (await db.Products.AnyAsync(ct))
            return; // idempotent — only seed once

        var products = BuildSeedProducts();
        db.Products.AddRange(products);

        // Publish CatalogSeeded via MassTransit transactional outbox
        //var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        //var seedId = Guid.NewGuid();
        //await publishEndpoint.Publish(new CatalogSeeded(
        //    MessageId: Guid.NewGuid(),
        //    CorrelationId: Guid.NewGuid(),
        //    CausationId: Guid.Empty,
        //    OccurredAt: DateTimeOffset.UtcNow,
        //    SeedId: seedId,
        //    ItemCount: products.Count,
        //    SeededAt: DateTimeOffset.UtcNow), ct);

        await db.SaveChangesAsync(ct); // commits product rows + outbox message atomically
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static List<Product> BuildSeedProducts() =>
    [
        // Electronics (8 products)
        new() { Id = Guid.NewGuid(), Name = "Wireless Noise-Cancelling Headphones", Sku = "ELEC-001", Description = "Premium over-ear headphones with active noise cancellation and 30-hour battery life.", Price = 299.99m, StockQuantity = 45, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "4K Ultra HD Smart TV 55-inch", Sku = "ELEC-002", Description = "55-inch 4K OLED smart TV with HDR10+ and built-in streaming apps.", Price = 899.99m, StockQuantity = 12, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Mechanical Gaming Keyboard", Sku = "ELEC-003", Description = "RGB mechanical keyboard with Cherry MX Red switches, ideal for gaming and typing.", Price = 129.99m, StockQuantity = 78, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Portable Bluetooth Speaker", Sku = "ELEC-004", Description = "Waterproof portable speaker with 360-degree sound and 20-hour battery.", Price = 79.99m, StockQuantity = 150, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Wireless Charging Pad", Sku = "ELEC-005", Description = "15W fast wireless charger compatible with Qi-enabled devices.", Price = 29.99m, StockQuantity = 200, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "USB-C Hub 7-in-1", Sku = "ELEC-006", Description = "Multi-port USB-C hub with HDMI, USB-A, SD card reader, and 100W PD charging.", Price = 49.99m, StockQuantity = 95, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Ergonomic Wireless Mouse", Sku = "ELEC-007", Description = "Vertical ergonomic mouse with adjustable DPI and silent clicks.", Price = 59.99m, StockQuantity = 110, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Smart Home Hub", Sku = "ELEC-008", Description = "Central smart home controller compatible with Zigbee, Z-Wave, and Wi-Fi devices.", Price = 149.99m, StockQuantity = 33, Category = "Electronics", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },

        // Clothing (7 products)
        new() { Id = Guid.NewGuid(), Name = "Classic Fit Oxford Shirt", Sku = "CLTH-001", Description = "100% cotton classic-fit Oxford shirt, available in white, blue, and grey.", Price = 49.99m, StockQuantity = 85, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Slim Fit Chino Trousers", Sku = "CLTH-002", Description = "Stretch-cotton slim-fit chinos with concealed waistband adjustment.", Price = 64.99m, StockQuantity = 60, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Merino Wool Sweater", Sku = "CLTH-003", Description = "Lightweight merino wool crew-neck sweater, naturally temperature-regulating.", Price = 89.99m, StockQuantity = 40, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Running Performance Jacket", Sku = "CLTH-004", Description = "Lightweight wind-resistant jacket with reflective details for low-light running.", Price = 119.99m, StockQuantity = 25, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Casual Canvas Sneakers", Sku = "CLTH-005", Description = "Classic low-top canvas sneakers in natural colours with rubber sole.", Price = 44.99m, StockQuantity = 120, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Insulated Winter Puffer Jacket", Sku = "CLTH-006", Description = "Down-filled puffer jacket with water-resistant shell, rated to -15°C.", Price = 179.99m, StockQuantity = 18, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Bamboo Blend Socks (5-Pack)", Sku = "CLTH-007", Description = "Eco-friendly bamboo-cotton blend ankle socks, antibacterial and breathable.", Price = 19.99m, StockQuantity = 200, Category = "Clothing", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },

        // Books (6 products)
        new() { Id = Guid.NewGuid(), Name = "Clean Code: A Handbook of Agile Software Craftsmanship", Sku = "BOOK-001", Description = "Robert C. Martin's definitive guide to writing readable, maintainable code.", Price = 34.99m, StockQuantity = 55, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Designing Data-Intensive Applications", Sku = "BOOK-002", Description = "Martin Kleppmann's comprehensive guide to building scalable and reliable systems.", Price = 44.99m, StockQuantity = 42, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Domain-Driven Design: Tackling Complexity", Sku = "BOOK-003", Description = "Eric Evans' foundational text on DDD patterns and bounded contexts.", Price = 39.99m, StockQuantity = 30, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "The Pragmatic Programmer", Sku = "BOOK-004", Description = "20th Anniversary Edition — timeless lessons for software developers.", Price = 29.99m, StockQuantity = 68, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Microservices Patterns", Sku = "BOOK-005", Description = "Chris Richardson's patterns for developing and deploying microservices.", Price = 36.99m, StockQuantity = 38, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Building Microservices, 2nd Edition", Sku = "BOOK-006", Description = "Sam Newman's updated guide covering service mesh, containers, and CI/CD.", Price = 42.99m, StockQuantity = 22, Category = "Books", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },

        // Home (5 products)
        new() { Id = Guid.NewGuid(), Name = "Bamboo Cutting Board Set", Sku = "HOME-001", Description = "Set of 3 organic bamboo cutting boards in small, medium, and large sizes.", Price = 34.99m, StockQuantity = 75, Category = "Home", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "French Press Coffee Maker", Sku = "HOME-002", Description = "34 oz borosilicate glass French press with stainless steel frame and double-wall filter.", Price = 39.99m, StockQuantity = 50, Category = "Home", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Ceramic Non-Stick Frying Pan 28cm", Sku = "HOME-003", Description = "PTFE-free ceramic-coated frying pan with stay-cool handle, oven-safe to 260°C.", Price = 54.99m, StockQuantity = 40, Category = "Home", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Memory Foam Mattress Topper", Sku = "HOME-004", Description = "5cm gel-infused memory foam topper with breathable bamboo cover, Queen size.", Price = 129.99m, StockQuantity = 15, Category = "Home", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Weighted Blanket 7kg", Sku = "HOME-005", Description = "Calming 7kg glass-bead weighted blanket for better sleep, machine washable.", Price = 89.99m, StockQuantity = 28, Category = "Home", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },

        // Sports (4 products)
        new() { Id = Guid.NewGuid(), Name = "Adjustable Dumbbell Set 5-25kg", Sku = "SPRT-001", Description = "Space-saving adjustable dumbbells with quick-select dial, replaces 9 pairs.", Price = 349.99m, StockQuantity = 8, Category = "Sports", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Yoga Mat Premium 6mm", Sku = "SPRT-002", Description = "Non-slip TPE yoga mat with alignment lines and carrying strap, 183×61cm.", Price = 44.99m, StockQuantity = 90, Category = "Sports", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Resistance Band Set (5 Levels)", Sku = "SPRT-003", Description = "Set of 5 latex-free resistance bands from extra-light to extra-heavy.", Price = 24.99m, StockQuantity = 130, Category = "Sports", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Foam Roller High-Density 45cm", Sku = "SPRT-004", Description = "High-density EVA foam roller for muscle recovery and myofascial release.", Price = 29.99m, StockQuantity = 65, Category = "Sports", ImageUrl = null, CreatedAt = DateTimeOffset.UtcNow },
    ];
}
