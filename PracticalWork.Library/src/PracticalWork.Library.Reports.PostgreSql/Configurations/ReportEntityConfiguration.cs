using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Reports.PostgreSql.Entities;

namespace PracticalWork.Library.Reports.PostgreSql.Configurations;

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