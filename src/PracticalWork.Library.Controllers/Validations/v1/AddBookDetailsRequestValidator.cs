using FluentValidation;
using PracticalWork.Library.Contracts.v1.Books.Request;

namespace PracticalWork.Library.Controllers.Validations.v1;

public sealed class AddBookDetailsRequestValidator : AbstractValidator<AddBookDetailsRequest>
{
    public AddBookDetailsRequestValidator()
    {
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        var allowedMimeTypes = new[] 
        { 
            "image/png", 
            "image/jpeg", 
            "image/jpg", 
            "image/webp" 
        };
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание не может превышать 2000 символов.")
            .When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.CoverImage)
            .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("Файл слишком большой(>5MB)")
            .Must(f => !string.IsNullOrEmpty(f.FileName)).WithMessage("Название файла не должно быть пустым")
            .Must(f => allowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            .Must(f => allowedMimeTypes.Contains(Path.GetExtension(f.ContentType).ToLowerInvariant()))
            .WithMessage("Обложка должна быть формата jpeg/jpg/png/webp");
    }
}