using EduGuard.Application.DTOs.Classes;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class ClassService : IClassService
{
    private readonly IEduGuardDataStore _dataStore;

    public ClassService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<AcademicYearDto>.Fail("Academic year name is required.");
        }

        var year = new AcademicYear
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddAcademicYearAsync(year, cancellationToken);
        return ApiResponse<AcademicYearDto>.Ok(MapToYearDto(year), "Academic year created.");
    }

    public async Task<ApiResponse<List<AcademicYearDto>>> GetAcademicYearsAsync(CancellationToken cancellationToken = default)
    {
        var years = await _dataStore.GetAllAcademicYearsAsync(cancellationToken);
        return ApiResponse<List<AcademicYearDto>>.Ok(years.Select(MapToYearDto).ToList());
    }

    public async Task<ApiResponse<AcademicYearDto>> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default)
    {
        var year = await _dataStore.GetCurrentAcademicYearAsync(cancellationToken);
        if (year == null)
        {
            return ApiResponse<AcademicYearDto>.Fail("No academic year configured.");
        }
        return ApiResponse<AcademicYearDto>.Ok(MapToYearDto(year));
    }

    public async Task<ApiResponse<ClassGradeDto>> CreateGradeAsync(CreateGradeDto request, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(request.SchoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<ClassGradeDto>.Fail("School not found.");
        }

        var grade = new ClassGrade
        {
            Id = Guid.NewGuid(),
            SchoolId = request.SchoolId,
            GradeLevel = request.GradeLevel,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Grade {request.GradeLevel}" : request.Name.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddGradeAsync(grade, cancellationToken);
        return ApiResponse<ClassGradeDto>.Ok(new ClassGradeDto
        {
            Id = grade.Id,
            SchoolId = grade.SchoolId,
            GradeLevel = grade.GradeLevel,
            Name = grade.Name
        }, "Grade created successfully.");
    }

    public async Task<ApiResponse<List<ClassGradeDto>>> GetGradesBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var grades = await _dataStore.GetGradesBySchoolAsync(schoolId, cancellationToken);
        var dtos = new List<ClassGradeDto>();

        foreach (var grade in grades)
        {
            var sections = await _dataStore.GetSectionsByGradeAsync(grade.Id, null, cancellationToken);
            var sectionDtos = new List<SectionDto>();
            foreach (var s in sections)
            {
                var year = await _dataStore.GetAcademicYearByIdAsync(s.AcademicYearId, cancellationToken);
                var teacher = s.ClassTeacherUserId.HasValue ? await _dataStore.GetUserByIdAsync(s.ClassTeacherUserId.Value, cancellationToken) : null;
                var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(s.Id, cancellationToken);

                sectionDtos.Add(MapToSectionDto(s, grade, year, teacher, enrollments.Count));
            }

            dtos.Add(new ClassGradeDto
            {
                Id = grade.Id,
                SchoolId = grade.SchoolId,
                GradeLevel = grade.GradeLevel,
                Name = grade.Name,
                Sections = sectionDtos
            });
        }

        return ApiResponse<List<ClassGradeDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<SectionDto>> CreateSectionAsync(CreateSectionDto request, CancellationToken cancellationToken = default)
    {
        var grade = await _dataStore.GetGradeByIdAsync(request.ClassGradeId, cancellationToken);
        if (grade == null)
        {
            return ApiResponse<SectionDto>.Fail("Grade not found.");
        }

        var year = await _dataStore.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (year == null)
        {
            return ApiResponse<SectionDto>.Fail("Academic year not found.");
        }

        User? teacher = null;
        if (request.ClassTeacherUserId.HasValue)
        {
            teacher = await _dataStore.GetUserByIdAsync(request.ClassTeacherUserId.Value, cancellationToken);
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            ClassGradeId = request.ClassGradeId,
            AcademicYearId = request.AcademicYearId,
            SectionName = request.SectionName.Trim().ToUpperInvariant(),
            ClassTeacherUserId = request.ClassTeacherUserId,
            RoomNumber = request.RoomNumber,
            Capacity = request.Capacity,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddSectionAsync(section, cancellationToken);
        return ApiResponse<SectionDto>.Ok(MapToSectionDto(section, grade, year, teacher, 0), "Section created.");
    }

    public async Task<ApiResponse<List<SectionDto>>> GetSectionsByGradeAsync(Guid gradeId, Guid? academicYearId, CancellationToken cancellationToken = default)
    {
        var grade = await _dataStore.GetGradeByIdAsync(gradeId, cancellationToken);
        if (grade == null)
        {
            return ApiResponse<List<SectionDto>>.Fail("Grade not found.");
        }

        var sections = await _dataStore.GetSectionsByGradeAsync(gradeId, academicYearId, cancellationToken);
        var dtos = new List<SectionDto>();

        foreach (var s in sections)
        {
            var year = await _dataStore.GetAcademicYearByIdAsync(s.AcademicYearId, cancellationToken);
            var teacher = s.ClassTeacherUserId.HasValue ? await _dataStore.GetUserByIdAsync(s.ClassTeacherUserId.Value, cancellationToken) : null;
            var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(s.Id, cancellationToken);
            dtos.Add(MapToSectionDto(s, grade, year, teacher, enrollments.Count));
        }

        return ApiResponse<List<SectionDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<SectionDto>> GetSectionByIdAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(sectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<SectionDto>.Fail("Section not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        var year = await _dataStore.GetAcademicYearByIdAsync(section.AcademicYearId, cancellationToken);
        var teacher = section.ClassTeacherUserId.HasValue ? await _dataStore.GetUserByIdAsync(section.ClassTeacherUserId.Value, cancellationToken) : null;
        var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(section.Id, cancellationToken);

        return ApiResponse<SectionDto>.Ok(MapToSectionDto(section, grade!, year, teacher, enrollments.Count));
    }

    public async Task<ApiResponse> AssignClassTeacherAsync(AssignClassTeacherDto request, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse.FailResult("Section not found.");
        }

        if (request.TeacherUserId.HasValue)
        {
            var teacher = await _dataStore.GetUserByIdAsync(request.TeacherUserId.Value, cancellationToken);
            if (teacher == null)
            {
                return ApiResponse.FailResult("Teacher user not found.");
            }
        }

        section.ClassTeacherUserId = request.TeacherUserId;
        await _dataStore.UpdateSectionAsync(section, cancellationToken);
        return ApiResponse.OkResult("Class teacher assigned successfully.");
    }

    private static AcademicYearDto MapToYearDto(AcademicYear y) => new()
    {
        Id = y.Id,
        Name = y.Name,
        StartDate = y.StartDate,
        EndDate = y.EndDate,
        IsCurrent = y.IsCurrent
    };

    private static SectionDto MapToSectionDto(Section s, ClassGrade g, AcademicYear? y, User? teacher, int enrolledCount) => new()
    {
        Id = s.Id,
        ClassGradeId = s.ClassGradeId,
        GradeName = g.Name,
        GradeLevel = g.GradeLevel,
        AcademicYearId = s.AcademicYearId,
        AcademicYearName = y?.Name ?? string.Empty,
        SectionName = s.SectionName,
        ClassTeacherUserId = s.ClassTeacherUserId,
        ClassTeacherName = teacher?.FullName,
        RoomNumber = s.RoomNumber,
        Capacity = s.Capacity,
        EnrolledStudentsCount = enrolledCount
    };
}
