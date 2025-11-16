using API.DTOs;
using FluentValidation;

namespace API.Validators;

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => new[] { "Pending", "Paid", "Shipped", "Delivered", "pending", "paid", "shipped", "delivered" }.Contains(status))
            .WithMessage("Status must be Pending, Paid, Shipped, or Delivered");
    }
}

