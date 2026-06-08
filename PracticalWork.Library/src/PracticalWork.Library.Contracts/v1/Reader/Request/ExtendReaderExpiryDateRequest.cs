namespace PracticalWork.Library.Contracts.v1.Reader.Request;

/// <summary>
/// Запрос на продление карточки
/// </summary>
/// <param name="Date">срок продления</param>
public sealed record ExtendReaderExpiryDateRequest(DateOnly Date);