using AutoFixture;
using Moq;
using PracticalWork.Report.Enums;

namespace PracticalWork.Report.Tests.Domain;

public class ReportModelTests
{
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Fixture _fixture = new();
    private readonly DateTimeOffset _fixedDateTimeOffset;

    public ReportModelTests()
    {
        _timeProviderMock = new Mock<TimeProvider>();
        _fixedDateTimeOffset = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeOffset);
    }
    [Theory]
    [InlineData(ReportStatus.Error)]
    [InlineData(ReportStatus.InProgress)]
    public void MarkAsGenerated_Success_ShouldUpdateReportModelAsGenerated(ReportStatus status)
    {
        var report = _fixture
            .Build<Models.Report>()
            .Without(r => r.CreatedAt)
            .With(r => r.Status, status)
            .Without(r => r.Name)
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .Create();
        
        var reportName = _fixture.Create<string>();
        
        report.MarkAsGenerated(reportName, _timeProviderMock.Object);
        
        Assert.Equal(reportName, report.Name);
        Assert.Equal(_fixedDateTimeOffset.DateTime, report.GeneratedAt);
        Assert.Equal(ReportStatus.Generated, report.Status);
    }
}