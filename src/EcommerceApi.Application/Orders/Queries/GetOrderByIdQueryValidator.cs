using FluentValidation;

namespace EcommerceApi.Application.Orders.Queries;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
