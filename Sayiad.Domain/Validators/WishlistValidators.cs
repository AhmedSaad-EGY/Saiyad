using FluentValidation;
using Sayiad.Domain.Dtos.WishlistDtos;

namespace Sayiad.Domain.Validators;

public class ToggleWishlistValidator : AbstractValidator<ToggleWishlistRequest>
{
    public ToggleWishlistValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
