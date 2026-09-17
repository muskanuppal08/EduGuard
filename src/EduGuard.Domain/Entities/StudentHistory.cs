using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class StudentHistory : BaseEntity
{
    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; } = null!;

    public string EventType { get; set; } = string.Empty; // "Enrollment", "Promotion", "Dropout", "Note", "StatusChange"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime EventDateUtc { get; set; } = DateTime.UtcNow;
}
