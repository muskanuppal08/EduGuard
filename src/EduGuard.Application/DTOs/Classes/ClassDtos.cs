namespace EduGuard.Application.DTOs.Classes;

public class CreateAcademicYearDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; } = true;
}

public class AcademicYearDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class CreateGradeDto
{
    public Guid SchoolId { get; set; }
    public int GradeLevel { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ClassGradeDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public int GradeLevel { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SectionDto> Sections { get; set; } = new();
}

public class CreateSectionDto
{
    public Guid ClassGradeId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid? ClassTeacherUserId { get; set; }
    public string? RoomNumber { get; set; }
    public int Capacity { get; set; } = 40;
}

public class SectionDto
{
    public Guid Id { get; set; }
    public Guid ClassGradeId { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public int GradeLevel { get; set; }
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string FullDisplayName => $"{GradeName}-{SectionName}";
    public Guid? ClassTeacherUserId { get; set; }
    public string? ClassTeacherName { get; set; }
    public string? RoomNumber { get; set; }
    public int Capacity { get; set; }
    public int EnrolledStudentsCount { get; set; }
}

public class AssignClassTeacherDto
{
    public Guid SectionId { get; set; }
    public Guid? TeacherUserId { get; set; }
}
