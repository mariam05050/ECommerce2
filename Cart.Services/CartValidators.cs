using FluentValidation;

namespace Cart.Services;

public class AddToCartValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();
    }
}