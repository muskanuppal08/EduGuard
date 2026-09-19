using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class AbsenceAlert : BaseEntity
{
    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; } = null!;

    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public AbsencePatternType PatternType { get; set; }
    public string Severity { get; set; } = "Warning"; // "Warning", "Critical"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal MetricValue { get; set; } // e.g. 72.5% attendance or 4 days missed
    public decimal Threshold { get; set; }   // e.g. 85.0% threshold or 3 days streak

    public DateTime TriggeredDateUtc { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
