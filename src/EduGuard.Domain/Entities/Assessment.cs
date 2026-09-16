using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class Assessment : BaseEntity
{
    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public Guid SectionId { get; set; }
    public virtual Section Section { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public virtual Subject Subject { get; set; } = null!;

    public Guid AcademicYearId { get; set; }
    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public AssessmentCategory Category { get; set; } = AssessmentCategory.Midterm;
    public DateOnly ExamDate { get; set; }
    public decimal MaxMarks { get; set; } = 100.0m;
    public decimal PassingMarks { get; set; } = 35.0m;

    public virtual ICollection<StudentExamMark> Marks { get; set; } = new List<StudentExamMark>();
}
