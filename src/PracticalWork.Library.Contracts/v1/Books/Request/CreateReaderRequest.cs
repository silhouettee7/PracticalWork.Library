using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

public sealed record CreateReaderRequest(string FullName, string PhoneNumber, DateOnly ExpiryDate) 
    : AbstractReader(FullName, PhoneNumber, ExpiryDate);