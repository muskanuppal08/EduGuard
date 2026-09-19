namespace EduGuard.Application.DTOs.Academics;

public class SubjectScoreSummaryDto
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; }
    public decimal TotalMarksObtained { get; set; }
    public decimal TotalMaxMarks { get; set; }
    public decimal AveragePercentage => TotalMaxMarks > 0 ? Math.Round(TotalMarksObtained / TotalMaxMarks * 100m, 2) : 0.0m;
    public string OverallGrade { get; set; } = "F";
    public bool IsPass { get; set; }
    public int AssessmentsCount { get; set; }
}

public class StudentReportCardDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYearName { get; set; } = string.Empty;

    public List<SubjectScoreSummaryDto> SubjectSummaries { get; set; } = new();

    public decimal TotalMarksObtained { get; set; }
    public decimal TotalMaxMarks { get; set; }
    public decimal CumulativePercentage => TotalMaxMarks > 0 ? Math.Round(TotalMarksObtained / TotalMaxMarks * 100m, 2) : 0.0m;
    public decimal GPA { get; set; }
    public string OverallGrade { get; set; } = "F";

    // Critical Dropout Risk Indicators
    public int CoreSubjectsFailedCount { get; set; }
    public int TotalSubjectsFailedCount { get; set; }
    public bool IsAtAcademicRisk => CoreSubjectsFailedCount >= 1;
    public string AcademicRiskSeverity =>
        CoreSubjectsFailedCount >= 2 ? "Critical" :
        CoreSubjectsFailedCount == 1 ? "High" :
        TotalSubjectsFailedCount > 0 ? "Moderate" : "Low";
}

public class TermScorePointDto
{
    public string AssessmentTitle { get; set; } = string.Empty;
    public DateOnly ExamDate { get; set; }
    public decimal Percentage { get; set; }
}

public class AcademicTrajectoryDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public List<TermScorePointDto> TermPoints { get; set; } = new();
    public string Trajectory { get; set; } = "Stable"; // "Improving", "Stable", "Declining", "AcademicShock"
    public decimal MaxDropPercentage { get; set; }
    public bool HasAcademicShock => MaxDropPercentage >= 15.0m;
}

public class FailingStudentsSummaryDto
{
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; }
    public decimal PassingMarks { get; set; }
    public List<StudentMarkDto> FailingStudents { get; set; } = new();
}
