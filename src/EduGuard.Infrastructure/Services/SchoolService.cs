using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Schools;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class SchoolService : ISchoolService
{
    private readonly IEduGuardDataStore _dataStore;

    public SchoolService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<SchoolDto>> CreateSchoolAsync(CreateSchoolDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SchoolCode) || string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<SchoolDto>.Fail("School code and name are required.");
        }

        var existing = await _dataStore.GetSchoolByCodeAsync(request.SchoolCode, cancellationToken);
        if (existing != null)
        {
            return ApiResponse<SchoolDto>.Fail($"A school with code '{request.SchoolCode}' already exists.");
        }

        var school = new School
        {
            Id = Guid.NewGuid(),
            SchoolCode = request.SchoolCode.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            District = request.District.Trim(),
            BlockOrZone = request.BlockOrZone.Trim(),
            AreaType = request.AreaType,
            IsMarginalizedArea = request.IsMarginalizedArea,
            Address = request.Address,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            PrincipalName = request.PrincipalName,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddSchoolAsync(school, cancellationToken);
        return ApiResponse<SchoolDto>.Ok(MapToDto(school, 0, 0), "School created successfully.");
    }

    public async Task<ApiResponse<SchoolDto>> GetSchoolByIdAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(schoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<SchoolDto>.Fail("School not found.");
        }

        var studentCount = await _dataStore.CountStudentsAsync(s => s.SchoolId == schoolId, cancellationToken);
        var grades = await _dataStore.GetGradesBySchoolAsync(schoolId, cancellationToken);

        return ApiResponse<SchoolDto>.Ok(MapToDto(school, studentCount, grades.Count));
    }

    public async Task<ApiResponse<PagedResult<SchoolDto>>> GetSchoolsAsync(SchoolFilterDto filter, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        Func<School, bool> predicate = s =>
        {
            if (!string.IsNullOrWhiteSpace(filter.District) &&
                !s.District.Equals(filter.District, StringComparison.OrdinalIgnoreCase))
                return false;

            if (filter.AreaType.HasValue && s.AreaType != filter.AreaType.Value)
                return false;

            if (filter.IsMarginalizedArea.HasValue && s.IsMarginalizedArea != filter.IsMarginalizedArea.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                bool matches = s.Name.ToLowerInvariant().Contains(term) ||
                                s.SchoolCode.ToLowerInvariant().Contains(term) ||
                                s.District.ToLowerInvariant().Contains(term);
                if (!matches) return false;
            }

            return true;
        };

        var schools = await _dataStore.QuerySchoolsAsync(predicate, page, pageSize, cancellationToken);
        var total = await _dataStore.CountSchoolsAsync(predicate, cancellationToken);

        var dtos = new List<SchoolDto>();
        foreach (var school in schools)
        {
            var studentCount = await _dataStore.CountStudentsAsync(s => s.SchoolId == school.Id, cancellationToken);
            var grades = await _dataStore.GetGradesBySchoolAsync(school.Id, cancellationToken);
            dtos.Add(MapToDto(school, studentCount, grades.Count));
        }

        var result = new PagedResult<SchoolDto>(dtos, total, page, pageSize);
        return ApiResponse<PagedResult<SchoolDto>>.Ok(result);
    }

    public async Task<ApiResponse<SchoolDto>> UpdateSchoolAsync(Guid schoolId, UpdateSchoolDto request, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(schoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<SchoolDto>.Fail("School not found.");
        }

        school.Name = request.Name.Trim();
        school.District = request.District.Trim();
        school.BlockOrZone = request.BlockOrZone.Trim();
        school.AreaType = request.AreaType;
        school.IsMarginalizedArea = request.IsMarginalizedArea;
        school.Address = request.Address;
        school.ContactEmail = request.ContactEmail;
        school.ContactPhone = request.ContactPhone;
        school.PrincipalName = request.PrincipalName;

        await _dataStore.UpdateSchoolAsync(school, cancellationToken);

        var studentCount = await _dataStore.CountStudentsAsync(s => s.SchoolId == schoolId, cancellationToken);
        var grades = await _dataStore.GetGradesBySchoolAsync(schoolId, cancellationToken);

        return ApiResponse<SchoolDto>.Ok(MapToDto(school, studentCount, grades.Count), "School updated successfully.");
    }

    private static SchoolDto MapToDto(School s, int studentCount, int gradeCount)
    {
        return new SchoolDto
        {
            Id = s.Id,
            SchoolCode = s.SchoolCode,
            Name = s.Name,
            District = s.District,
            BlockOrZone = s.BlockOrZone,
            AreaType = s.AreaType,
            IsMarginalizedArea = s.IsMarginalizedArea,
            Address = s.Address,
            ContactEmail = s.ContactEmail,
            ContactPhone = s.ContactPhone,
            PrincipalName = s.PrincipalName,
            TotalStudents = studentCount,
            TotalClasses = gradeCount,
            CreatedAtUtc = s.CreatedAtUtc
        };
    }
}
