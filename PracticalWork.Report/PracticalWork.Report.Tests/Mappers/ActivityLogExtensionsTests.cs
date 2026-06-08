using AutoFixture;
using Domain.Events;
using Domain.Models;
using PracticalWork.Report.Controllers.Mappers.v1;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Tests.Mappers;

public class ActivityLogExtensionsTests
{
    private readonly Fixture _fixture = new();
    
    public ActivityLogExtensionsTests()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
    
    [Fact]
    public void ToActivityLogResponse_ShouldMapCorrectly()
    {
        // Arrange
        var activityLog = _fixture.Build<ActivityLog>()
            .With(a => a.Event, _fixture.Create<BookArchivedEvent>())
            .With(a => a.EventType, "BookCreated")
            .With(a => a.EventDate, new DateTime(2024, 1, 15, 10, 30, 0))
            .Create();
        
        // Act
        var result = activityLog.ToActivityLogResponse();
        
        // Assert
        Assert.Equal(activityLog.Event, result.Event);
        Assert.Equal(activityLog.EventType, result.EventType);
        Assert.Equal(activityLog.EventDate, result.EventDate);
    }
    
    [Fact]
    public void ToActivityLogsPaginationResponse_ShouldMapCorrectly()
    {
        // Arrange
        var activityLogs = _fixture.Build<ActivityLog>()
            .With(a => a.Event, _fixture.Create<BookArchivedEvent>())
            .Without(a => a.EventDate)
            .CreateMany(3)
            .ToList();
        
        var cursorResponse = new CursorPaginationResponse<ActivityLog>
        {
            Items = activityLogs,
            NextCursor = "next_cursor_123",
            PreviousCursor = "prev_cursor_456",
            HasNext = true,
            HasPrevious = false
        };
        
        // Act
        var result = cursorResponse.ToActivityLogsPaginationResponse();
        
        // Assert
        Assert.Equal(cursorResponse.NextCursor, result.NextCursor);
        Assert.Equal(cursorResponse.PreviousCursor, result.PreviousCursor);
        Assert.Equal(cursorResponse.HasNext, result.HasNext);
        Assert.Equal(cursorResponse.HasPrevious, result.HasPrevious);
        Assert.Equal(activityLogs.Count, result.Items.Count);
        
        for (int i = 0; i < activityLogs.Count; i++)
        {
            Assert.Equal(activityLogs[i].Event, result.Items[i].Event);
            Assert.Equal(activityLogs[i].EventType, result.Items[i].EventType);
            Assert.Equal(activityLogs[i].EventDate, result.Items[i].EventDate);
        }
    }
}