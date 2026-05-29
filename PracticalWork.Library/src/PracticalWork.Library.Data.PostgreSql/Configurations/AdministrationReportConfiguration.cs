using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Data.PostgreSql.Configurations;

internal class AdministrationReportConfiguration: EntityConfigurationBase<AdministrationReportEntity>
{
    public override void Configure(EntityTypeBuilder<AdministrationReportEntity> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Name)
            .HasMaxLength(255);
        
        builder.Property(e => e.FilePath)
            .HasMaxLength(500);
        
        builder.Property(e => e.Status)
            .HasConversion<string>(
                rs => rs.ToString(),
                s => Enum.Parse<AdministrationReportStatus>(s));
    }
}