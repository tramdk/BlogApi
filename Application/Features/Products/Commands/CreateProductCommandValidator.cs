using FluentValidation;

namespace FloraCore.Application.Features.Products.Commands;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.Description).NotEmpty();
        RuleFor(v => v.Price).GreaterThan(0);
        RuleFor(v => v.Stock).GreaterThanOrEqualTo(0);
        RuleFor(v => v.PromotionRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Promotion Rate must be between 0 and 100.");
    }
}
