using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Reader.Request;

/// <summary>
/// Запрос на создание карточки
/// </summary>
/// <param name="FullName">полное имя</param>
/// <param name="PhoneNumber">номер телефона</param>
/// <param name="ExpiryDate">срок действия</param>
public sealed record CreateReaderRequest(string FullName, string PhoneNumber, DateOnly ExpiryDate) 
    : AbstractReader(FullName, PhoneNumber, ExpiryDate);