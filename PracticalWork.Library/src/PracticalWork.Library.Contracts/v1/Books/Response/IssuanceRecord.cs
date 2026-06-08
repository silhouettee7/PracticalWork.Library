using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

/// <summary>
/// Запись о выдаче
/// </summary>
/// <param name="IssueStatus">статус выдачи</param>
/// <param name="DueDate">до какой даты выдана</param>
/// <param name="ReturnDate">дата возвращения</param>
/// <param name="BorrowDate">дата выдачи</param>
public sealed record IssuanceRecord(
    BookIssueStatus IssueStatus,
    DateOnly DueDate,
    DateOnly ReturnDate,
    DateOnly BorrowDate
);