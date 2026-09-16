using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IAbsencePatternDetector
{
    Task<ApiResponse<List<ChronicAbsenteeDto>>> DetectChronicAbsenteesAsync(Guid schoolId, decimal threshold = 85.0m, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AbsenceAlertDto>>> RunAbsencePatternAnalysisForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AbsenceAlertDto>>> GetActiveAlertsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<ApiResponse> ResolveAlertAsync(ResolveAlertDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
}
