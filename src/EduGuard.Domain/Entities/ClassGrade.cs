using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class ClassGrade : BaseEntity
{
    public Guid SchoolId { get; set; }
    public virtual School School { get; set; } = null!;

    public int GradeLevel { get; set; } // e.g. 6, 7, 8, 9, 10
    public string Name { get; set; } = string.Empty; // e.g. "Grade 9"

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}
