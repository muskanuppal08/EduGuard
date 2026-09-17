using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class Section : BaseEntity
{
    public Guid ClassGradeId { get; set; }
    public virtual ClassGrade ClassGrade { get; set; } = null!;

    public Guid AcademicYearId { get; set; }
    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public string SectionName { get; set; } = string.Empty; // e.g. "A", "B"
    public Guid? ClassTeacherUserId { get; set; }
    public string? RoomNumber { get; set; }
    public int Capacity { get; set; } = 40;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
