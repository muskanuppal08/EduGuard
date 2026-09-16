using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IAttendanceAnalyticsService
{
    Task<ApiResponse<StudentAttendanceSummaryDto>> GetStudentAttendanceSummaryAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StudentAttendanceCalendarDto>> GetStudentAttendanceCalendarAsync(Guid studentId, int month, int year, CancellationToken cancellationToken = default);
    Task<ApiResponse<SectionAttendanceStatsDto>> GetSectionDailyStatsAsync(Guid sectionId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceTrendDto>> GetStudentAttendanceTrendAsync(Guid studentId, int pastMonths = 6, CancellationToken cancellationToken = default);
}
