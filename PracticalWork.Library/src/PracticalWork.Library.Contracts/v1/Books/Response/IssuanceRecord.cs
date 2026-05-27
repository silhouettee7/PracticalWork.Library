using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public sealed record IssuanceRecord(
    BookIssueStatus IssueStatus,
    DateOnly DueDate,
    DateOnly ReturnDate,
    DateOnly BorrowDate
);