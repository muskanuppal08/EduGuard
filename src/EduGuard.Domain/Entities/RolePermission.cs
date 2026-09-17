using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public string Permission { get; set; } = string.Empty;
}
