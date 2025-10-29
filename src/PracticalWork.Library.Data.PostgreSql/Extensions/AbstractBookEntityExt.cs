using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Extensions;

public static class AbstractBookEntityExt
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
            IsArchived = bookEntity.Status == BookStatus.Archived,
            Category = bookEntity.Category,
            Cursor = new Cursor {Id = bookEntity.Id}
        };
    
}