namespace EduGuard.Application.DTOs.History;

public class StudentTimelineItemDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime EventDateUtc { get; set; }
    public Guid? RecordedByUserId { get; set; }
}

public class AddTimelineNoteDto
{
    public Guid StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
