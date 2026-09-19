using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class AttendanceAnalyticsService : IAttendanceAnalyticsService
{
    private readonly IEduGuardDataStore _dataStore;

    public AttendanceAnalyticsService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<StudentAttendanceSummaryDto>> GetStudentAttendanceSummaryAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentAttendanceSummaryDto>.Fail("Student not found.");
        }

        var records = await _dataStore.GetAttendanceByStudentAsync(studentId, null, null, cancellationToken);
        var summary = CalculateSummary(student.Id, student.AdmissionNumber, student.FullName, records);

        return ApiResponse<StudentAttendanceSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<StudentAttendanceCalendarDto>> GetStudentAttendanceCalendarAsync(Guid studentId, int month, int year, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentAttendanceCalendarDto>.Fail("Student not found.");
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = await _dataStore.GetAttendanceByStudentAsync(studentId, startDate, endDate, cancellationToken);

        var days = records.Select(r => new AttendanceCalendarDayDto
        {
            Date = r.Date,
            Status = r.Status.ToString(),
            Reason = r.Reason.ToString(),
            Remarks = r.Remarks
        }).ToList();

        var result = new StudentAttendanceCalendarDto
        {
            StudentId = student.Id,
            FullName = student.FullName,
            Month = month,
            Year = year,
            Days = days
        };

        return ApiResponse<StudentAttendanceCalendarDto>.Ok(result);
    }

    public async Task<ApiResponse<SectionAttendanceStatsDto>> GetSectionDailyStatsAsync(Guid sectionId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(sectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<SectionAttendanceStatsDto>.Fail("Section not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(sectionId, cancellationToken);
        var records = await _dataStore.GetAttendanceBySectionAndDateAsync(sectionId, date, cancellationToken);

        int present = records.Count(r => r.Status == AttendanceStatus.Present);
        int absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        int excused = records.Count(r => r.Status == AttendanceStatus.Excused);
        int late = records.Count(r => r.Status == AttendanceStatus.Late);
        int halfDay = records.Count(r => r.Status == AttendanceStatus.HalfDay);

        int totalMarked = records.Count;
        decimal rate = totalMarked > 0
            ? Math.Round(((decimal)present + (0.5m * halfDay)) / totalMarked * 100m, 2)
            : 0.0m;

        var stats = new SectionAttendanceStatsDto
        {
            SectionId = section.Id,
            SectionName = $"{grade?.Name}-{section.SectionName}",
            Date = date,
            TotalEnrolled = enrollments.Count,
            PresentCount = present,
            AbsentCount = absent,
            ExcusedCount = excused,
            LateCount = late,
            HalfDayCount = halfDay,
            DailyAttendanceRate = rate
        };

        return ApiResponse<SectionAttendanceStatsDto>.Ok(stats);
    }

    public async Task<ApiResponse<AttendanceTrendDto>> GetStudentAttendanceTrendAsync(Guid studentId, int pastMonths = 6, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<AttendanceTrendDto>.Fail("Student not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-pastMonths);
        var records = await _dataStore.GetAttendanceByStudentAsync(studentId, startDate, today, cancellationToken);

        var monthlyGroups = records
            .GroupBy(r => new { r.Date.Year, r.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .ToList();

        var points = new List<MonthlyAttendanceTrendPointDto>();
        foreach (var group in monthlyGroups)
        {
            int total = group.Count();
            int present = group.Count(r => r.Status == AttendanceStatus.Present);
            int halfDay = group.Count(r => r.Status == AttendanceStatus.HalfDay);

            decimal rate = total > 0
                ? Math.Round(((decimal)present + 0.5m * halfDay) / total * 100m, 2)
                : 0.0m;

            var dt = new DateTime(group.Key.Year, group.Key.Month, 1);
            points.Add(new MonthlyAttendanceTrendPointDto
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                MonthName = dt.ToString("MMM yyyy"),
                AttendanceRate = rate,
                TotalDays = total
            });
        }

        string trajectory = "Stable";
        if (points.Count >= 2)
        {
            var firstRate = points[0].AttendanceRate;
            var lastRate = points[^1].AttendanceRate;
            decimal delta = lastRate - firstRate;

            if (delta <= -15.0m) trajectory = "CriticalDrop";
            else if (delta < -5.0m) trajectory = "Declining";
            else if (delta > 5.0m) trajectory = "Improving";
        }

        var trend = new AttendanceTrendDto
        {
            EntityId = student.Id,
            EntityName = student.FullName,
            MonthlyPoints = points,
            Trajectory = trajectory
        };

        return ApiResponse<AttendanceTrendDto>.Ok(trend);
    }

    public static StudentAttendanceSummaryDto CalculateSummary(
        Guid studentId,
        string admissionNumber,
        string fullName,
        IReadOnlyList<Domain.Entities.AttendanceRecord> records)
    {
        int total = records.Count;
        int present = records.Count(r => r.Status == AttendanceStatus.Present);
        int absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        int excused = records.Count(r => r.Status == AttendanceStatus.Excused);
        int late = records.Count(r => r.Status == AttendanceStatus.Late);
        int halfDay = records.Count(r => r.Status == AttendanceStatus.HalfDay);

        decimal overallPct = total > 0
            ? Math.Round(((decimal)present + (0.5m * halfDay)) / total * 100m, 2)
            : 100.0m;

        // Rolling 30 days calculation
        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var rollingRecords = records.Where(r => r.Date >= thirtyDaysAgo).ToList();
        int rollingTotal = rollingRecords.Count;
        int rollingPresent = rollingRecords.Count(r => r.Status == AttendanceStatus.Present);
        int rollingHalfDay = rollingRecords.Count(r => r.Status == AttendanceStatus.HalfDay);

        decimal rollingPct = rollingTotal > 0
            ? Math.Round(((decimal)rollingPresent + (0.5m * rollingHalfDay)) / rollingTotal * 100m, 2)
            : overallPct;

        // Calculate consecutive absent streak ending at most recent date
        int consecutiveAbsent = 0;
        var sortedDesc = records.OrderByDescending(r => r.Date).ToList();
        foreach (var r in sortedDesc)
        {
            if (r.Status == AttendanceStatus.Absent)
            {
                consecutiveAbsent++;
            }
            else
            {
                break;
            }
        }

        return new StudentAttendanceSummaryDto
        {
            StudentId = studentId,
            AdmissionNumber = admissionNumber,
            FullName = fullName,
            TotalWorkingDays = total,
            DaysPresent = present,
            DaysAbsent = absent,
            DaysExcused = excused,
            DaysLate = late,
            DaysHalfDay = halfDay,
            AttendancePercentage = overallPct,
            Rolling30DayPercentage = rollingPct,
            ConsecutiveAbsentDays = consecutiveAbsent
        };
    }
}
