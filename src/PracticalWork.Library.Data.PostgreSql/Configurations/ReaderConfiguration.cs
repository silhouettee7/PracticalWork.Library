using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticalWork.Library.Data.PostgreSql.Entities;

namespace PracticalWork.Library.Data.PostgreSql.Configurations;

internal sealed class ReaderConfiguration : EntityConfigurationBase<ReaderEntity>
{
    public override void Configure(EntityTypeBuilder<ReaderEntity> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(12)
            .IsRequired();

        builder.HasMany(c => c.BorrowedRecords)
            .WithOne()
            .HasForeignKey(p => p.ReaderId);

        builder.HasIndex(r => r.PhoneNumber).IsUnique();
    }
}