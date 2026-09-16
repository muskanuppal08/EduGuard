namespace EduGuard.Application.DTOs.Academics;

public class CreateSubjectDto
{
    public Guid SchoolId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; } = true;
    public string? Description { get; set; }
}

public class SubjectDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCoreSubject { get; set; }
    public string? Description { get; set; }
}

public class SubjectFilterDto
{
    public Guid? SchoolId { get; set; }
    public bool? IsCoreSubject { get; set; }
    public string? SearchTerm { get; set; }
}
