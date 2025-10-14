namespace PracticalWork.Library.Contracts.v1.Enums;

/// <summary>
/// Тип книги
/// </summary>
public enum BookCategory
{
    Default = 0,

    /// <summary>Научная литература</summary>
    ScientificBook = 10,

    /// <summary>Учебное пособие</summary>
    EducationalBook = 20,

    /// <summary>Художественная литература</summary>
    FictionBook = 30
}