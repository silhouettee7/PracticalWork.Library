using Domain.Models;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Extensions;

public static class MapEntitiesExt
{
    public static Book ToBook(this AbstractBookEntity bookEntity) => 
        new()
        {
            Authors = bookEntity.Authors,
            Description = bookEntity.Description,
            CoverImagePath = bookEntity.CoverImagePath,
            Status = bookEntity.Status,
            Title = bookEntity.Title,
            Year = bookEntity.Year,
            Category = bookEntity.Category,
            Cursor = new Cursor {Id = bookEntity.Id},
            IssuanceRecords = bookEntity.IssuanceRecords?
                .Select(b => b.ToBookBorrow())
                .ToList() ?? new List<BookBorrow>(),
        };

    public static BookBorrow ToBookBorrow(this BookBorrowEntity entity) =>
        new()
        {
            Status = entity.Status,
            ReturnDate = entity.ReturnDate ?? default,
            DueDate = entity.DueDate,
            BorrowDate = entity.BorrowDate,
            Book = new Book
            {
                Status = entity.Book.Status,
            }
        };

}