using EduGuard.Domain.Common;

namespace EduGuard.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? SchoolId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; } = false;
    public DateTime? LockoutEndUtc { get; set; }
    public int AccessFailedCount { get; set; } = 0;
    public DateTime? LastLoginAtUtc { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual UserProfile? Profile { get; set; }
}
