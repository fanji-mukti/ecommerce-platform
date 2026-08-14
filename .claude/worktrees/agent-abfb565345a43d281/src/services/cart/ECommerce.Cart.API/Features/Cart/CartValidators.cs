using FluentValidation;

namespace ECommerce.Cart.API.Features.Cart;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}

public class UpdateCartItemQuantityRequestValidator : AbstractValidator<UpdateCartItemQuantityRequest>
{
    public UpdateCartItemQuantityRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}
