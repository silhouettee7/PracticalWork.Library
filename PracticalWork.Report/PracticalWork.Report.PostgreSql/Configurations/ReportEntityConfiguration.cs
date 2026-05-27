using Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticalWork.Report.PostgreSql.Entities;

namespace PracticalWork.Report.PostgreSql.Configurations;

public class ReportEntityConfiguration: EntityConfigurationBase<ReportEntity>
{
    public override void Configure(EntityTypeBuilder<ReportEntity> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Name)
            .HasMaxLength(255);
        
        builder.Property(e => e.FilePath)
            .HasMaxLength(500);
        
        builder.Property(e => e.Status)
            .HasConversion<string>(
                rs => rs.ToString(),
                s => Enum.Parse<ReportStatus>(s));
    }
}