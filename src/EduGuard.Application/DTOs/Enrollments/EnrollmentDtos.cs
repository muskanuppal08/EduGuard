using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Enrollments;

public class EnrollStudentDto
{
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public Guid AcademicYearId { get; set; }
    public DateOnly? EnrollmentDate { get; set; }
    public string? Remarks { get; set; }
}

public class BatchPromoteDto
{
    public Guid FromSectionId { get; set; }
    public Guid ToSectionId { get; set; }
    public Guid NewAcademicYearId { get; set; }
    public List<Guid> StudentIds { get; set; } = new();
}

public class RecordDropoutDto
{
    public Guid StudentId { get; set; }
    public DateOnly DropoutDate { get; set; }
    public DropoutReason Reason { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class TransferStudentDto
{
    public Guid StudentId { get; set; }
    public Guid NewSchoolId { get; set; }
    public Guid? NewSectionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReEnrollDto
{
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
    public StudentStatus Status { get; set; }
    public DateOnly? ExitDate { get; set; }
    public DropoutReason ExitReason { get; set; }
    public string? Remarks { get; set; }
}
