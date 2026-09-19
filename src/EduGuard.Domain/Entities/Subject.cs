using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class Subject : BaseEntity
{
    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public string SubjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; } = true; // Math, Science, Language predict dropout
    public string? Description { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
