using PracticalWork.Library.Dtos;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Extensions;

public static class BorrowedIssuedBookInfoDtoExt
{
    public static BorrowedBookNotification ToBorrowedBookNotification(this BorrowedIssuedBookInfoDto dto, TimeProvider timeProvider)
    {
        TimeSpan diff = dto.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - timeProvider.GetUtcNow().Date;
        return new BorrowedBookNotification
        {
            BookTitle = dto.BookTitle,
            Authors = dto.Authors,
            ReaderFullName = dto.ReaderFullName,
            ReturnDate = dto.DueDate,
            DaysCountBeforeReturn = (byte) diff.TotalDays
        };
    }
}