using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Attendance;

public class AbsenceAlertDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public AbsencePatternType PatternType { get; set; }
    public string PatternTypeName => PatternType.ToString();
    public string Severity { get; set; } = "Warning";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public decimal Threshold { get; set; }
    public DateTime TriggeredDateUtc { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class ResolveAlertDto
{
    public Guid AlertId { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class ChronicAbsenteeDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public decimal AttendancePercentage { get; set; }
    public int MissedDays { get; set; }
    public int TotalDays { get; set; }
    public bool IsBPL { get; set; }
    public decimal DistanceToSchoolKm { get; set; }
    public string? GuardianPhone { get; set; }
}
