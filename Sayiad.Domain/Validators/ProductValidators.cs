using FluentValidation;
using Sayiad.Data.Models;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Domain.Validators;

public class AddProductImageValidator : AbstractValidator<AddProductImageRequest>
{
    public AddProductImageValidator()
    {
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(2048);
    }
}

public class RejectProductValidator : AbstractValidator<RejectProductRequest>
{
    public RejectProductValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class UpdateProductStatusValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class ProductFilterValidator : AbstractValidator<ProductFilterRequest>
{
}
