namespace EduGuard.Application.DTOs.Common;

public class StudentRiskDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public decimal AttendancePercentage { get; set; }
    public decimal AcademicAverage { get; set; }
    public bool AttendanceDataAvailable { get; set; }
    public bool AcademicDataAvailable { get; set; }
    public List<string> Factors { get; set; } = new();
}

public class DropoutAnalyticsSummaryDto
{
    public int TotalStudents { get; set; }
    public int CriticalRisk { get; set; }
    public int HighRisk { get; set; }
    public int ModerateRisk { get; set; }
    public int LowRisk { get; set; }
    public int Unassessed { get; set; }
    public decimal AverageRiskScore { get; set; }
    public List<StudentRiskDto> Students { get; set; } = new();
}

public class InterventionDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Moderate";
    public string? FollowUpNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? FollowUpAtUtc { get; set; }
}

public class CreateInterventionDto
{
    public Guid StudentId { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Priority { get; set; } = "Moderate";
    public DateTime? FollowUpAtUtc { get; set; }
}

public class UpdateInterventionDto
{
    public string Status { get; set; } = "Open";
    public string? FollowUpNotes { get; set; }
    public DateTime? FollowUpAtUtc { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Audience { get; set; } = "All";
    public bool Read { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { set; get; } = string.Empty;
    public string Audience { get; set; } = "Parents";
}

public class ModelTrainingDto
{
    public DateTime TrainedAtUtc { get; set; }
    public int TrainingRecords { get; set; }
    public decimal Accuracy { get; set; }
    public decimal Recall { get; set; }
    public decimal Precision { get; set; }
    public decimal F1Score { get; set; }
    public string Status { get; set; } = "Ready";
}

public class SupportRequestDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string Urgency { get; set; } = "Standard";
    public string Description { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Under Review";
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateSupportRequestDto
{
    public Guid StudentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string Urgency { get; set; } = "Standard";
    public string Description { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
}

public class UpdateSupportRequestDto
{
    public string Status { get; set; } = "Under Review";
    public string ResolutionNotes { get; set; } = string.Empty;
}
