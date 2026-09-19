using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.AttendanceRead)]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceAnalyticsService _analyticsService;
    private readonly IAbsencePatternDetector _patternDetector;

    public AttendanceController(
        IAttendanceService attendanceService,
        IAttendanceAnalyticsService analyticsService,
        IAbsencePatternDetector patternDetector)
    {
        _attendanceService = attendanceService;
        _analyticsService = analyticsService;
        _patternDetector = patternDetector;
    }

    /// <summary>
    /// Retrieves the attendance roster for a section on a given date for marking or review.
    /// </summary>
    [HttpGet("roster")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRosterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceRoster([FromQuery] Guid sectionId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetAttendanceRosterAsync(sectionId, date, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Records or updates bulk daily attendance for a section and automatically evaluates absence alerts.
    /// </summary>
    [HttpPost("batch")]
    [RequirePermission(SystemPermission.AttendanceRecord)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordDailyAttendanceBatch([FromBody] RecordDailyAttendanceBatchDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.RecordDailyAttendanceBatchAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Updates an individual student's attendance record with absence reason.
    /// </summary>
    [HttpPut("{recordId:guid}")]
    [RequirePermission(SystemPermission.AttendanceRecord)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttendanceRecord(Guid recordId, [FromBody] UpdateAttendanceItemDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.UpdateAttendanceRecordAsync(recordId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Calculates cumulative attendance percentage, missed days, and chronic absenteeism status for a student.
    /// </summary>
    [HttpGet("student/{studentId:guid}/summary")]
    [ProducesResponseType(typeof(ApiResponse<StudentAttendanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentAttendanceSummary(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetStudentAttendanceSummaryAsync(studentId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Returns the month-by-month calendar view of student attendance.
    /// </summary>
    [HttpGet("student/{studentId:guid}/calendar")]
    [ProducesResponseType(typeof(ApiResponse<StudentAttendanceCalendarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentAttendanceCalendar(Guid studentId, [FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetStudentAttendanceCalendarAsync(studentId, month, year, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Analyzes long-term attendance trajectory and detects sharp drops over time.
    /// </summary>
    [HttpGet("student/{studentId:guid}/trend")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentAttendanceTrend(Guid studentId, [FromQuery] int pastMonths = 6, CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetStudentAttendanceTrendAsync(studentId, pastMonths, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Gets section-level daily attendance rate and summary counts.
    /// </summary>
    [HttpGet("section/{sectionId:guid}/stats")]
    [ProducesResponseType(typeof(ApiResponse<SectionAttendanceStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionDailyStats(Guid sectionId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSectionDailyStatsAsync(sectionId, date, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Detects all students in a school whose cumulative attendance is under the chronic absenteeism threshold (< 85%).
    /// </summary>
    [HttpGet("alerts/chronic-absentees")]
    [RequirePermission(SystemPermission.AttendanceAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<List<ChronicAbsenteeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectChronicAbsentees([FromQuery] Guid schoolId, [FromQuery] decimal threshold = 85.0m, CancellationToken cancellationToken = default)
    {
        var result = await _patternDetector.DetectChronicAbsenteesAsync(schoolId, threshold, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all active, unresolved absence pattern alerts for a school (chronic absentees, streaks, sudden drops).
    /// </summary>
    [HttpGet("alerts/school/{schoolId:guid}")]
    [RequirePermission(SystemPermission.AttendanceAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<List<AbsenceAlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSchoolAlerts(Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _patternDetector.GetActiveAlertsBySchoolAsync(schoolId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resolves an absence pattern alert with counselor/teacher action notes.
    /// </summary>
    [HttpPost("alerts/resolve")]
    [RequirePermission(SystemPermission.AttendanceAnalytics)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveAlert([FromBody] ResolveAlertDto request, CancellationToken cancellationToken)
    {
        var result = await _patternDetector.ResolveAlertAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
