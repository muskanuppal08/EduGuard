using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IAcademicService
{
    Task<ApiResponse<SubjectDto>> CreateSubjectAsync(CreateSubjectDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SubjectDto>>> GetSubjectsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<ApiResponse<SubjectDto>> GetSubjectByIdAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<ApiResponse<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssessmentDto>>> GetAssessmentsAsync(AssessmentFilterDto filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssessmentDto>> GetAssessmentByIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}
