using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; } = null!;

    public Guid SectionId { get; set; }
    public virtual Section Section { get; set; } = null!;

    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public AbsenceReason Reason { get; set; } = AbsenceReason.None;
    public string? Remarks { get; set; }

    public Guid? RecordedByUserId { get; set; }
}
