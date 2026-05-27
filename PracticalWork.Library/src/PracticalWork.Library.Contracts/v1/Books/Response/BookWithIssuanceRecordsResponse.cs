using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public sealed record BookWithIssuanceRecordsResponse(
    string Title,
    BookCategory Category,
    IReadOnlyList<string> Authors,
    string Description,
    int Year,
    BookStatus Status,
    bool IsArchived,
    IReadOnlyList<IssuanceRecord> Records
    ) : AbstractBook(Title, Authors, Description, Year);