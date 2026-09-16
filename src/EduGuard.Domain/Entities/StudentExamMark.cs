using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class StudentExamMark : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public virtual Assessment Assessment { get; set; } = null!;

    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; } = null!;

    public decimal MarksObtained { get; set; } = 0.0m;
    public bool IsAbsent { get; set; } = false;
    public string GradeLetter { get; set; } = "F";
    public decimal GradePoint { get; set; } = 0.0m;
    public bool IsPass { get; set; } = false;
    public string? Remarks { get; set; }

    public Guid? RecordedByUserId { get; set; }
}
