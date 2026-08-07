using Riok.Mapperly.Abstractions;

namespace ECommerce.Orders.API.Features.Orders;

[Mapper]
public partial class OrderMapper
{
    public partial OrderSummaryDto ToSummaryDto(OrderReadModel order);

    public partial OrderDto ToDto(OrderReadModel order);

    private partial OrderLineItemDto ToLineItemDto(OrderLineItem lineItem);
}
