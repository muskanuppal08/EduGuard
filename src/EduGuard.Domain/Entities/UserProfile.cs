using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
}
