namespace PracticalWork.Library.Contracts.v1.Abstracts;

/// <summary>
/// дто читателя
/// </summary>
/// <param name="FullName">полное имя</param>
/// <param name="PhoneNumber">номер телефона</param>
/// <param name="ExpiryDate">срок действия карточки</param>
public abstract record AbstractReader(string FullName, string PhoneNumber, DateOnly ExpiryDate);