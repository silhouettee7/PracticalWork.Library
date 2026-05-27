using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public record BorrowedBookResponse(
    string Title,
    BookCategory Category,
    IReadOnlyList<string> Authors,
    string Description,
    int Year,
    BookIssueStatus IssueStatus,
    DateOnly DueDate,
    DateOnly ReturnDate,
    DateOnly BorrowDate
    );