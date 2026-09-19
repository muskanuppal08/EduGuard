using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Academics;

public class CreateAssessmentDto
{
    public Guid SchoolId { get; set; }
    public Guid SectionId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string Title { get; set; } = string.Empty;
    public AssessmentCategory Category { get; set; } = AssessmentCategory.Midterm;
    public DateOnly ExamDate { get; set; }
    public decimal MaxMarks { get; set; } = 100.0m;
    public decimal PassingMarks { get; set; } = 35.0m;
}

public class AssessmentDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; }
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AssessmentCategory Category { get; set; }
    public string CategoryName => Category.ToString();
    public DateOnly ExamDate { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal PassingMarks { get; set; }
    public int EvaluatedStudentsCount { get; set; }
}

public class AssessmentFilterDto
{
    public Guid? SchoolId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public AssessmentCategory? Category { get; set; }
}
