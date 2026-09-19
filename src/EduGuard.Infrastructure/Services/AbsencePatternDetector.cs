using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class AbsencePatternDetector : IAbsencePatternDetector
{
    private readonly IEduGuardDataStore _dataStore;

    public AbsencePatternDetector(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<List<ChronicAbsenteeDto>>> DetectChronicAbsenteesAsync(Guid schoolId, decimal threshold = 85.0m, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(schoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<List<ChronicAbsenteeDto>>.Fail("School not found.");
        }

        var students = await _dataStore.QueryStudentsAsync(s => s.SchoolId == schoolId && s.CurrentStatus == StudentStatus.Active, 1, 1000, cancellationToken);
        var result = new List<ChronicAbsenteeDto>();

        foreach (var student in students)
        {
            var records = await _dataStore.GetAttendanceByStudentAsync(student.Id, null, null, cancellationToken);
            if (records.Count < 5) continue; // Need minimal attendance history

            var summary = AttendanceAnalyticsService.CalculateSummary(student.Id, student.AdmissionNumber, student.FullName, records);

            if (summary.AttendancePercentage < threshold)
            {
                string className = "N/A";
                if (student.CurrentSectionId.HasValue)
                {
                    var section = await _dataStore.GetSectionByIdAsync(student.CurrentSectionId.Value, cancellationToken);
                    if (section != null)
                    {
                        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                        className = $"{grade?.Name}-{section.SectionName}";
                    }
                }

                result.Add(new ChronicAbsenteeDto
                {
                    StudentId = student.Id,
                    AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName,
                    ClassName = className,
                    AttendancePercentage = summary.AttendancePercentage,
                    MissedDays = summary.DaysAbsent,
                    TotalDays = summary.TotalWorkingDays,
                    IsBPL = student.IsBPL,
                    DistanceToSchoolKm = student.DistanceToSchoolKm,
                    GuardianPhone = student.GuardianPhone
                });
            }
        }

        return ApiResponse<List<ChronicAbsenteeDto>>.Ok(result.OrderBy(r => r.AttendancePercentage).ToList());
    }

    public async Task<ApiResponse<List<AbsenceAlertDto>>> RunAbsencePatternAnalysisForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<List<AbsenceAlertDto>>.Fail("Student not found.");
        }

        var records = await _dataStore.GetAttendanceByStudentAsync(studentId, null, null, cancellationToken);
        if (records.Count == 0)
        {
            return ApiResponse<List<AbsenceAlertDto>>.Ok(new List<AbsenceAlertDto>());
        }

        var summary = AttendanceAnalyticsService.CalculateSummary(student.Id, student.AdmissionNumber, student.FullName, records);
        var existingAlerts = await _dataStore.GetAlertsByStudentAsync(studentId, cancellationToken);
        var school = await _dataStore.GetSchoolByIdAsync(student.SchoolId, cancellationToken);
        string schoolName = school?.Name ?? string.Empty;

        var triggeredAlerts = new List<AbsenceAlertDto>();

        // 1. Check Consecutive Absence Streak (>= 3 days)
        if (summary.ConsecutiveAbsentDays >= 3)
        {
            var existingStreakAlert = existingAlerts
                .FirstOrDefault(a => a.PatternType == AbsencePatternType.ConsecutiveAbsenceStreak && !a.IsResolved);

            if (existingStreakAlert == null)
            {
                string severity = summary.ConsecutiveAbsentDays >= 5 ? "Critical" : "Warning";
                var alert = new AbsenceAlert
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    SchoolId = student.SchoolId,
                    PatternType = AbsencePatternType.ConsecutiveAbsenceStreak,
                    Severity = severity,
                    Title = $"{summary.ConsecutiveAbsentDays}-Day Consecutive Absence Alert",
                    Description = $"Student has been absent for {summary.ConsecutiveAbsentDays} consecutive unexcused days. Immediate teacher/counselor contact advised.",
                    MetricValue = summary.ConsecutiveAbsentDays,
                    Threshold = 3,
                    TriggeredDateUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _dataStore.AddAbsenceAlertAsync(alert, cancellationToken);
                triggeredAlerts.Add(MapToAlertDto(alert, student.FullName, student.AdmissionNumber, schoolName));

                // Log into student history timeline
                await _dataStore.AddHistoryItemAsync(new StudentHistory
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    EventType = "AbsenceAlert",
                    Title = alert.Title,
                    Description = alert.Description,
                    EventDateUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        // 2. Check Chronic Absenteeism (< 85% with >= 10 records)
        if (summary.TotalWorkingDays >= 10 && summary.AttendancePercentage < 85.0m)
        {
            var existingChronicAlert = existingAlerts
                .FirstOrDefault(a => a.PatternType == AbsencePatternType.ChronicAbsenteeism && !a.IsResolved);

            if (existingChronicAlert == null)
            {
                var alert = new AbsenceAlert
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    SchoolId = student.SchoolId,
                    PatternType = AbsencePatternType.ChronicAbsenteeism,
                    Severity = summary.AttendancePercentage < 75.0m ? "Critical" : "Warning",
                    Title = "Chronic Absenteeism Flag",
                    Description = $"Overall attendance rate has dropped to {summary.AttendancePercentage}% ({summary.DaysAbsent} missed days out of {summary.TotalWorkingDays}). High dropout risk indicator.",
                    MetricValue = summary.AttendancePercentage,
                    Threshold = 85.0m,
                    TriggeredDateUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _dataStore.AddAbsenceAlertAsync(alert, cancellationToken);
                triggeredAlerts.Add(MapToAlertDto(alert, student.FullName, student.AdmissionNumber, schoolName));

                await _dataStore.AddHistoryItemAsync(new StudentHistory
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    EventType = "ChronicAbsenteeism",
                    Title = alert.Title,
                    Description = alert.Description,
                    EventDateUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        return ApiResponse<List<AbsenceAlertDto>>.Ok(triggeredAlerts);
    }

    public async Task<ApiResponse<List<AbsenceAlertDto>>> GetActiveAlertsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var alerts = await _dataStore.GetActiveAlertsBySchoolAsync(schoolId, cancellationToken);
        var dtos = new List<AbsenceAlertDto>();

        foreach (var a in alerts)
        {
            var student = await _dataStore.GetStudentByIdAsync(a.StudentId, cancellationToken);
            var school = await _dataStore.GetSchoolByIdAsync(a.SchoolId, cancellationToken);
            dtos.Add(MapToAlertDto(a, student?.FullName ?? string.Empty, student?.AdmissionNumber ?? string.Empty, school?.Name ?? string.Empty));
        }

        return ApiResponse<List<AbsenceAlertDto>>.Ok(dtos);
    }

    public async Task<ApiResponse> ResolveAlertAsync(ResolveAlertDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var alert = await _dataStore.GetAlertByIdAsync(request.AlertId, cancellationToken);
        if (alert == null)
        {
            return ApiResponse.FailResult("Alert not found.");
        }

        alert.IsResolved = true;
        alert.ResolutionNotes = request.ResolutionNotes;
        alert.ResolvedByUserId = operatorUserId;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        await _dataStore.UpdateAbsenceAlertAsync(alert, cancellationToken);

        // Record resolution in student history
        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = alert.StudentId,
            EventType = "AlertResolved",
            Title = $"Alert Resolved: {alert.Title}",
            Description = $"Resolution note: {request.ResolutionNotes}",
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse.OkResult("Alert marked as resolved.");
    }

    private static AbsenceAlertDto MapToAlertDto(AbsenceAlert a, string studentName, string admissionNumber, string schoolName) => new()
    {
        Id = a.Id,
        StudentId = a.StudentId,
        StudentName = studentName,
        AdmissionNumber = admissionNumber,
        SchoolId = a.SchoolId,
        SchoolName = schoolName,
        PatternType = a.PatternType,
        Severity = a.Severity,
        Title = a.Title,
        Description = a.Description,
        MetricValue = a.MetricValue,
        Threshold = a.Threshold,
        TriggeredDateUtc = a.TriggeredDateUtc,
        IsResolved = a.IsResolved,
        ResolutionNotes = a.ResolutionNotes,
        ResolvedAtUtc = a.ResolvedAtUtc
    };
}
