namespace PracticalWork.Library.Exceptions;

/// <summary>
/// Исключение уровня сервиса с хранилищем, если не найден файл
/// </summary>
public class FileNotFoundException: NotFoundException
{
    public FileNotFoundException(string message) : base(message)
    {
    }
}