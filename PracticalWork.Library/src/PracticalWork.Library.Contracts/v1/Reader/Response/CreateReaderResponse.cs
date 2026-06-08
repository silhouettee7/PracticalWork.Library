namespace PracticalWork.Library.Contracts.v1.Reader.Response;

/// <summary>
/// Ответ на создание карточки
/// </summary>
/// <param name="Id">идентификатор карточки</param>
public sealed record CreateReaderResponse(Guid Id);