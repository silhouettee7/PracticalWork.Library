using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PracticalWork.Library.Controllers.Filters;

public class FileImageValidationFilter: IAsyncActionFilter
{
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FileImageValidationFilter(long maxFileSizeBytes, params string[] allowedExtensions)
    {
        _maxFileSize = maxFileSizeBytes;
        _allowedExtensions = allowedExtensions;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var files = context.HttpContext.Request.Form.Files;
        
        foreach (var file in files)
        {
            await ValidateFileAsync(file);
        }

        await next();
    }
    private async Task ValidateFileAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Файл не может быть пустым");
        }

        if (file.Length > _maxFileSize)
        {
            throw new ValidationException($"Размер файла не должен превышать {_maxFileSize / 1024 / 1024}MB");
        }
        
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(fileExtension))
        {
            var allowed = string.Join(", ", _allowedExtensions);
            throw new ValidationException($"Недопустимый формат файла. Разрешены: {allowed}");
        }

        await ValidateMimeTypeAsync(file);
    }
    private async Task ValidateMimeTypeAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var buffer = new byte[20];
        
        var mimeType = GetMimeType(buffer, file.FileName);
        
        var allowedMimeTypes = new Dictionary<string, string[]>
        {
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" },
            [".webp"] = new[] { "image/webp" }
        };

        var expectedMimeTypes = allowedMimeTypes[Path.GetExtension(file.FileName).ToLowerInvariant()];
        if (!expectedMimeTypes.Contains(mimeType))
        {
            throw new ValidationException($"Несоответствие MIME типа файла. Ожидался: {string.Join(", ", expectedMimeTypes)}");
        }
    }

    private string GetMimeType(byte[] fileBytes, string fileName)
    {
        if (fileBytes.Length >= 2)
        {
            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8)
                return "image/jpeg";

            if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
                return "image/png";

            if (fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46)
                return "image/webp";
        }
        return "unknown";
    }
}