using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class Enrollment : BaseEntity
{
    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; } = null!;

    public Guid SectionId { get; set; }
    public virtual Section Section { get; set; } = null!;

    public Guid AcademicYearId { get; set; }
    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public DateOnly EnrollmentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public DateOnly? ExitDate { get; set; }
    public DropoutReason ExitReason { get; set; } = DropoutReason.None;
    public string? Remarks { get; set; }
}
