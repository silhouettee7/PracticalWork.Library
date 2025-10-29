using FluentValidation;
using PracticalWork.Library.Contracts.v1.Books.Request;

namespace PracticalWork.Library.Controllers.Validations.v1;

public class ExtendReaderExpiryDateRequestValidator: 
    AbstractValidator<ExtendReaderExpiryDateRequest>
{
    public ExtendReaderExpiryDateRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().NotNull().WithMessage("Дата продления карточки обязательна")
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}