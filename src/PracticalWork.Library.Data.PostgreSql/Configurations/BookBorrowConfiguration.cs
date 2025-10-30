using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticalWork.Library.Data.PostgreSql.Entities;

namespace PracticalWork.Library.Data.PostgreSql.Configurations;

internal sealed class BookBorrowConfiguration : EntityConfigurationBase<BookBorrowEntity>
{
    public override void Configure(EntityTypeBuilder<BookBorrowEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Book)
            .WithMany(b => b.IssuanceRecords)
            .HasForeignKey(e => e.BookId);
    }
}