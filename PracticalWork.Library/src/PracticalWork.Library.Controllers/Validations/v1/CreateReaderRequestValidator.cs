using FluentValidation;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Reader.Request;

namespace PracticalWork.Library.Controllers.Validations.v1;

public class CreateReaderRequestValidator: AbstractValidator<CreateReaderRequest>
{
    public CreateReaderRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона обязателен")
            .Matches("^\\+?\\d{1,3}[\\s-]?\\(?\\d{3}\\)?[\\s-]?\\d{3}[\\s-]?\\d{2}[\\s-]?\\d{2}$")
            .WithMessage("Неправильный формат телефона");
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно").NotNull().WithMessage("ФИО обязательно")
            .MaximumLength(50).WithMessage("Имя не должно превышать 50 символов");
        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("Дата окончания карточки обязательна")
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}