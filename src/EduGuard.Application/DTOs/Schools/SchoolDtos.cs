using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Schools;

public class CreateSchoolDto
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
}

public class UpdateSchoolDto
{
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string BlockOrZone { get; set; } = string.Empty;
    public AreaType AreaType { get; set; }
    public bool IsMarginalizedArea { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? PrincipalName { get; set; }
}

public class SchoolDto
{
    public Guid Id { get; set; }
    public string SchoolCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string BlockOrZone { get; set; } = string.Empty;
    public AreaType AreaType { get; set; }
    public bool IsMarginalizedArea { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? PrincipalName { get; set; }
    public int TotalStudents { get; set; }
    public int TotalClasses { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class SchoolFilterDto
{
    public string? District { get; set; }
    public AreaType? AreaType { get; set; }
    public bool? IsMarginalizedArea { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
