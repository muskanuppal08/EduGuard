using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class Student : BaseEntity
{
    // Identification
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty; // "Male", "Female", "Other"
    public string? MotherTongue { get; set; }
    public string? SocialCategory { get; set; }

    // Socioeconomic Vulnerability Factors (Dropout Risk Predictors)
    public bool IsBPL { get; set; } = false; // Below Poverty Line
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

    // Academic Affiliation & Current Status
    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public Guid? CurrentSectionId { get; set; }
    public virtual Section? CurrentSection { get; set; }

    public StudentStatus CurrentStatus { get; set; } = StudentStatus.Active;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<StudentHistory> HistoryTimeline { get; set; } = new List<StudentHistory>();
}
