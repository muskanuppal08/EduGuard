namespace EduGuard.Application.DTOs.Academics;

public class RecordStudentMarkItemDto
{
    public Guid StudentId { get; set; }
    public decimal MarksObtained { get; set; }
    public bool IsAbsent { get; set; } = false;
    public string? Remarks { get; set; }
}

public class RecordMarksBatchDto
{
    public Guid AssessmentId { get; set; }
    public List<RecordStudentMarkItemDto> Marks { get; set; } = new();
}

public class UpdateStudentMarkDto
{
    public decimal MarksObtained { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }
}

public class StudentMarkDto
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public decimal MarksObtained { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal PassingMarks { get; set; }
    public decimal Percentage => MaxMarks > 0 ? Math.Round(MarksObtained / MaxMarks * 100m, 2) : 0.0m;
    public string GradeLetter { get; set; } = "F";
    public decimal GradePoint { get; set; }
    public bool IsPass { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }
}

public class ExamRosterItemDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? MarkId { get; set; }
    public decimal? MarksObtained { get; set; }
    public bool IsAbsent { get; set; }
    public string? GradeLetter { get; set; }
    public bool? IsPass { get; set; }
    public string? Remarks { get; set; }
    public bool IsEvaluated => MarkId.HasValue;
}

public class ExamRosterDto
{
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal MaxMarks { get; set; }
    public decimal PassingMarks { get; set; }
    public int TotalStudents { get; set; }
    public int EvaluatedCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal PassPercentage => EvaluatedCount > 0 ? Math.Round((decimal)PassedCount / EvaluatedCount * 100m, 2) : 0.0m;
    public List<ExamRosterItemDto> Students { get; set; } = new();
}
