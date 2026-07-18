namespace ECommerce.Catalog.API.Features.Products;

public static class PaginationHelper
{
    /// <summary>
    /// Clamps pagination parameters to valid bounds.
    /// Page below 1 is clamped to 1.
    /// PageSize below 1 or above 100 is clamped to the default (12).
    /// </summary>
    public static (int page, int pageSize) Clamp(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 12;
        return (page, pageSize);
    }
}
