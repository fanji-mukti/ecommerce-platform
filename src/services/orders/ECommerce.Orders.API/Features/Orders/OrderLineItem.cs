namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// The one and only line-item CLR type in the Orders service. Configured as an owned type
/// independently by both <see cref="Order"/> (write side) and <see cref="OrderReadModel"/>
/// (read side) via two separate <c>OwnsMany</c> calls in OrdersDbContext — producing two
/// distinct owned tables from this single shared class (see OrdersDbContext.cs).
/// </summary>
public class OrderLineItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
