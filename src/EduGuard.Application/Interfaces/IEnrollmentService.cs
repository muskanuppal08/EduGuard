using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Enrollments;

namespace EduGuard.Application.Interfaces;

public interface IEnrollmentService
{
    Task<ApiResponse<EnrollmentDto>> EnrollStudentAsync(EnrollStudentDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> BatchPromoteStudentsAsync(BatchPromoteDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> RecordDropoutAsync(RecordDropoutDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> TransferStudentAsync(TransferStudentDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> ReEnrollStudentAsync(ReEnrollDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<EnrollmentDto>>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<EnrollmentDto>>> GetSectionEnrollmentsAsync(Guid sectionId, CancellationToken cancellationToken = default);
}
