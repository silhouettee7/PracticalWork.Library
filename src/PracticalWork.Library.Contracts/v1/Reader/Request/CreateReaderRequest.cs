using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Reader.Request;

public sealed record CreateReaderRequest(string FullName, string PhoneNumber, DateOnly ExpiryDate) 
    : AbstractReader(FullName, PhoneNumber, ExpiryDate);