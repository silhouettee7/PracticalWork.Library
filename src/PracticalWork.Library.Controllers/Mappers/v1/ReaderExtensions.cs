using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class ReaderExtensions
{
    public static Reader ToReader(this CreateReaderRequest createReaderRequest) =>
        new()
        {
            PhoneNumber = createReaderRequest.PhoneNumber,
            FullName = createReaderRequest.FullName,
            ExpiryDate = createReaderRequest.ExpiryDate,
        };
}