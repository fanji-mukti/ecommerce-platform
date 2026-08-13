namespace ECommerce.Orders.API.Features.Orders;

/// <summary>Summary shape for GET /orders (list). Never exposes saga-internal fields.</summary>
public record OrderSummaryDto(Guid Id, string Status, decimal TotalAmount, int ItemCount, DateTimeOffset CreatedAt);

/// <summary>Detail shape for GET /orders/{id}. Never exposes saga-internal fields.</summary>
public record OrderDto(Guid Id, string Status, decimal TotalAmount, List<OrderLineItemDto> LineItems, DateTimeOffset CreatedAt, string? FailureReason);

public record OrderLineItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
