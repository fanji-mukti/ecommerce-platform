namespace ECommerce.Cart.API.Features.Cart;

// Deliberately no price or name fields — the client never supplies price (P1 prohibition).
// Those are always populated server-side from ICatalogPriceClient.
public record AddCartItemRequest(Guid ProductId, int Quantity);

public record UpdateCartItemQuantityRequest(int Quantity);
