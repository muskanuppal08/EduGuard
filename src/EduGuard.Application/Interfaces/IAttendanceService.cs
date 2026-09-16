using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IAttendanceService
{
    Task<ApiResponse<AttendanceRosterDto>> GetAttendanceRosterAsync(Guid sectionId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RecordDailyAttendanceBatchAsync(RecordDailyAttendanceBatchDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceRecordDto>> UpdateAttendanceRecordAsync(Guid recordId, UpdateAttendanceItemDto request, CancellationToken cancellationToken = default);
}
