using FluentValidation;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ShippingAddressId).GreaterThan(0);
    }
}

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
