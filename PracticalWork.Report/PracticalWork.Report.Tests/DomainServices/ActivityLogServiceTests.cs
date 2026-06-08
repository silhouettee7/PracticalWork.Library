using AutoFixture;
using Domain.Abstractions.Services;
using Domain.Models;
using Moq;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.Application.Services;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Tests.DomainServices;

public class ActivityLogServiceTests
{
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly Mock<ICursorPaginationService<ActivityLog>> _cursorPaginationServiceMock;
    private readonly Fixture _fixture = new();
    private readonly ActivityLogService _activityLogService;

    public ActivityLogServiceTests()
    {
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _cursorPaginationServiceMock = new Mock<ICursorPaginationService<ActivityLog>>();
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _activityLogService = new ActivityLogService(
            _activityLogRepositoryMock.Object,
            _cursorPaginationServiceMock.Object);
    }
    
    [Fact]
    public async Task ReadSystemActivityLogs_Success_ShouldReturnPaginatedResponse()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        var eventDateFrom = new DateOnly(2024, 01, 01);
        var eventDateTo = new DateOnly(2024, 12, 31);
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(10)
            .ToList();
        
        var expectedResponse = _fixture
            .Build<CursorPaginationResponse<ActivityLog>>()
            .With(c => c.Items, logs)
            .Create();
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsPageAsync(request, eventDateFrom, eventDateTo, eventTypes))
            .ReturnsAsync(logs);
        
        _cursorPaginationServiceMock
            .Setup(x => x.ToCursorPageResponse(logs, request))
            .Returns(expectedResponse);
        
        // Act
        var result = await _activityLogService.ReadSystemActivityLogs(
            request, eventTypes, eventDateFrom, eventDateTo);
        
        // Assert
        Assert.Equivalent(result, expectedResponse);
    }
    
    [Fact]
    public async Task ReadSystemActivityLogs_Success_ShouldInvokeCursorPaginationService()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        var eventDateFrom = new DateOnly(2024, 01, 01);
        var eventDateTo = new DateOnly(2024, 12, 31);
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(10)
            .ToList();
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsPageAsync(request, eventDateFrom, eventDateTo, eventTypes))
            .ReturnsAsync(logs);
        
        // Act
        await _activityLogService.ReadSystemActivityLogs(
            request, eventTypes, eventDateFrom, eventDateTo);
        
        // Assert
        _cursorPaginationServiceMock.Verify(
            x => x.ToCursorPageResponse(logs, request), 
            Times.Once);
    }
    
    [Fact]
    public async Task ReadSystemActivityLogs_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        var eventDateFrom = new DateOnly(2024, 01, 01);
        var eventDateTo = new DateOnly(2024, 12, 31);
        
        var dbException = new Exception("Database connection failed");
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsPageAsync(request, eventDateFrom, eventDateTo, eventTypes))
            .ThrowsAsync(dbException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _activityLogService.ReadSystemActivityLogs(
                request, eventTypes, eventDateFrom, eventDateTo));
        
        Assert.Equal("Database connection failed", exception.Message);
        
        _cursorPaginationServiceMock.Verify(
            x => x.ToCursorPageResponse(It.IsAny<IReadOnlyList<ActivityLog>>(), It.IsAny<CursorPaginationRequest>()), 
            Times.Never);
    }
}