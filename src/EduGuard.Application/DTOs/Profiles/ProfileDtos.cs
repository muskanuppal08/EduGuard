namespace EduGuard.Application.DTOs.Profiles;

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? SchoolId { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class UserFilterDto
{
    public Guid? SchoolId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
