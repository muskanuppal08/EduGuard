using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IPerformanceAnalyticsService
{
    Task<ApiResponse<StudentReportCardDto>> GenerateReportCardAsync(Guid studentId, Guid? academicYearId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<AcademicTrajectoryDto>> GetAcademicTrajectoryAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<FailingStudentsSummaryDto>> GetFailingStudentsForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<StudentReportCardDto>>> GetAtRiskAcademicStudentsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);
}
