using EduGuard.Application.DTOs.Classes;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IClassService
{
    Task<ApiResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AcademicYearDto>>> GetAcademicYearsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<AcademicYearDto>> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<ClassGradeDto>> CreateGradeAsync(CreateGradeDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ClassGradeDto>>> GetGradesBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<ApiResponse<SectionDto>> CreateSectionAsync(CreateSectionDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SectionDto>>> GetSectionsByGradeAsync(Guid gradeId, Guid? academicYearId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SectionDto>> GetSectionByIdAsync(Guid sectionId, CancellationToken cancellationToken = default);
    Task<ApiResponse> AssignClassTeacherAsync(AssignClassTeacherDto request, CancellationToken cancellationToken = default);
}
