using Riok.Mapperly.Abstractions;

namespace ECommerce.Catalog.API.Features.Products;

[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product product);
}
