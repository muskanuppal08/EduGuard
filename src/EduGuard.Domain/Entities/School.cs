using EduGuard.Domain.Common;
using EduGuard.Domain.Enums;

namespace EduGuard.Domain.Entities;

public class School : BaseEntity
{
    public string SchoolCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string BlockOrZone { get; set; } = string.Empty;
    public AreaType AreaType { get; set; } = AreaType.Rural;
    public bool IsMarginalizedArea { get; set; } = true;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? PrincipalName { get; set; }

    public virtual ICollection<ClassGrade> Grades { get; set; } = new List<ClassGrade>();
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
