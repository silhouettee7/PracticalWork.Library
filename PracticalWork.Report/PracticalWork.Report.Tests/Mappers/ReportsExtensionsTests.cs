using AutoFixture;
using PracticalWork.Report.Controllers.Mappers.v1;

namespace PracticalWork.Report.Tests.Mappers;

public class ReportsExtensionsTests
{
    private readonly Fixture _fixture = new();
    
    public ReportsExtensionsTests()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
    
    [Fact]
    public void ToReportResponse_ShouldMapCorrectly()
    {
        // Arrange
        var report = _fixture.Build<Models.Report>()
            .With(r => r.Name)
            .With(r => r.FilePath)
            .With(r => r.PeriodFrom, new DateOnly(2024, 1, 1))
            .With(r => r.PeriodTo, new DateOnly(2024, 1, 31))
            .With(r => r.EventTypes, new[] { "BookCreated", "BookBorrowed" })
            .With(r => r.GeneratedAt, new DateTime(2024, 1, 15, 10, 30, 0))
            .Create();
        
        // Act
        var result = report.ToReportResponse();
        
        // Assert
        Assert.Equal(report.Name, result.ReportName);
        Assert.Equal(report.FilePath, result.FilePath);
        Assert.Equal(report.PeriodFrom, result.PeriodFrom);
        Assert.Equal(report.PeriodTo, result.PeriodTo);
        Assert.Equal(report.EventTypes, result.EventTypes);
        Assert.Equal(report.GeneratedAt!.Value, result.GeneratedAt);
    }
}