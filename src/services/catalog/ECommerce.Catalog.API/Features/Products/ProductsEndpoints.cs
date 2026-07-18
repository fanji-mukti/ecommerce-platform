using ECommerce.Catalog.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.API.Features.Products;

public static class ProductsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/products", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] int page,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? category,
            CatalogDbContext db,
            CancellationToken ct) =>
        {
            // Input validation — clamp bounds (ASVS V5, T-02-03-02)
            (page, pageSize) = PaginationHelper.Clamp(page, pageSize);

            var query = db.Products.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            var total = await query.CountAsync(ct);
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity, p.Category, p.ImageUrl))
                .ToListAsync(ct);

            return Results.Ok(new { Items = products, TotalCount = total, Page = page, PageSize = pageSize });
        });

        app.MapGet("/products/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync([id], ct);
            return product is null
                ? Results.NotFound(new { error = "Product not found." })
                : Results.Ok(new ProductDto(product.Id, product.Name, product.Sku, product.Price,
                    product.StockQuantity, product.Category, product.ImageUrl));
        });
    }
}
