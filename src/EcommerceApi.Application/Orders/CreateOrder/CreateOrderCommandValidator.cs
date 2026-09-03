using FluentValidation;

namespace EcommerceApi.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty();

        RuleFor(command => command.Items)
            .NotNull()
            .NotEmpty();

        RuleForEach(command => command.Items)
            .NotNull()
            .ChildRules(item =>
            {
                item.RuleFor(commandItem => commandItem!.ProductName)
                    .NotEmpty();
                item.RuleFor(commandItem => commandItem!.Quantity)
                    .GreaterThan(0);
                item.RuleFor(commandItem => commandItem!.UnitPrice)
                    .GreaterThan(0);
            });
    }
}
