using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Students;

public class RegisterStudentDto
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string? MotherTongue { get; set; }
    public string? SocialCategory { get; set; }

    // Socioeconomic Vulnerability Factors
    public bool IsBPL { get; set; } = false;
    public bool IsFirstGenerationLearner { get; set; } = false;
    public bool HasSpecialNeeds { get; set; } = false;
    public bool IsSingleParentOrOrphan { get; set; } = false;
    public decimal DistanceToSchoolKm { get; set; } = 0.0m;
    public TransportMode TransportMode { get; set; } = TransportMode.Walking;
    public FamilyIncomeTier FamilyIncomeTier { get; set; } = FamilyIncomeTier.Low;

    // Guardian Information
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? ResidentialAddress { get; set; }

    // Initial School & Section Assignment
    public Guid SchoolId { get; set; }
    public Guid? InitialSectionId { get; set; }
    public Guid? AcademicYearId { get; set; }
}

public class UpdateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string? MotherTongue { get; set; }
    public string? SocialCategory { get; set; }
    public bool IsBPL { get; set; }
    public bool IsFirstGenerationLearner { get; set; }
    public bool HasSpecialNeeds { get; set; }
    public bool IsSingleParentOrOrphan { get; set; }
    public decimal DistanceToSchoolKm { get; set; }
    public TransportMode TransportMode { get; set; }
    public FamilyIncomeTier FamilyIncomeTier { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? ResidentialAddress { get; set; }
}

public class StudentDto
{
    public Guid Id { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateOnly DateOfBirth { get; set; }
    public int Age => (int)Math.Floor((DateTime.UtcNow - DateOfBirth.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25);
    public string Gender { get; set; } = string.Empty;
    public string? MotherTongue { get; set; }
    public string? SocialCategory { get; set; }

    // Socioeconomic Flags
    public bool IsBPL { get; set; }
    public bool IsFirstGenerationLearner { get; set; }
    public bool HasSpecialNeeds { get; set; }
    public bool IsSingleParentOrOrphan { get; set; }
    public decimal DistanceToSchoolKm { get; set; }
    public TransportMode TransportMode { get; set; }
    public FamilyIncomeTier FamilyIncomeTier { get; set; }

    // Guardian Details
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? ResidentialAddress { get; set; }

    // Affiliations
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public Guid? CurrentSectionId { get; set; }
    public string? CurrentClassName { get; set; }
    public StudentStatus CurrentStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class StudentFilterDto
{
    public Guid? SchoolId { get; set; }
    public Guid? SectionId { get; set; }
    public StudentStatus? Status { get; set; }
    public string? Gender { get; set; }
    public bool? IsBPL { get; set; }
    public bool? IsFirstGenerationLearner { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
