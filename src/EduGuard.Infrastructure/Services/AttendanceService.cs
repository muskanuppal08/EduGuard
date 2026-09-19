using EduGuard.Application.DTOs.Attendance;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IEduGuardDataStore _dataStore;
    private readonly IAbsencePatternDetector _patternDetector;

    public AttendanceService(IEduGuardDataStore dataStore, IAbsencePatternDetector patternDetector)
    {
        _dataStore = dataStore;
        _patternDetector = patternDetector;
    }

    public async Task<ApiResponse<AttendanceRosterDto>> GetAttendanceRosterAsync(Guid sectionId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(sectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<AttendanceRosterDto>.Fail("Section not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(sectionId, cancellationToken);
        var existingRecords = await _dataStore.GetAttendanceBySectionAndDateAsync(sectionId, date, cancellationToken);
        var recordMap = existingRecords.ToDictionary(r => r.StudentId);

        var studentItems = new List<StudentRosterAttendanceItemDto>();
        int present = 0, absent = 0, late = 0, excused = 0, halfDay = 0;

        foreach (var enrollment in enrollments)
        {
            var student = await _dataStore.GetStudentByIdAsync(enrollment.StudentId, cancellationToken);
            if (student == null) continue;

            recordMap.TryGetValue(student.Id, out var existing);

            if (existing != null)
            {
                switch (existing.Status)
                {
                    case AttendanceStatus.Present: present++; break;
                    case AttendanceStatus.Absent: absent++; break;
                    case AttendanceStatus.Late: late++; break;
                    case AttendanceStatus.Excused: excused++; break;
                    case AttendanceStatus.HalfDay: halfDay++; break;
                }
            }

            studentItems.Add(new StudentRosterAttendanceItemDto
            {
                StudentId = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                FullName = student.FullName,
                AttendanceRecordId = existing?.Id,
                Status = existing?.Status,
                Reason = existing?.Reason,
                Remarks = existing?.Remarks
            });
        }

        var roster = new AttendanceRosterDto
        {
            SectionId = section.Id,
            SectionName = $"{grade?.Name}-{section.SectionName}",
            Date = date,
            TotalStudents = studentItems.Count,
            PresentCount = present,
            AbsentCount = absent,
            LateCount = late,
            ExcusedCount = excused,
            HalfDayCount = halfDay,
            Students = studentItems.OrderBy(s => s.FullName).ToList()
        };

        return ApiResponse<AttendanceRosterDto>.Ok(roster);
    }

    public async Task<ApiResponse<int>> RecordDailyAttendanceBatchAsync(RecordDailyAttendanceBatchDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<int>.Fail("Section not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        var schoolId = grade?.SchoolId ?? Guid.Empty;

        int recordedCount = 0;
        var atRiskStudentIds = new HashSet<Guid>();

        foreach (var item in request.Records)
        {
            var student = await _dataStore.GetStudentByIdAsync(item.StudentId, cancellationToken);
            if (student == null) continue;

            var existing = (await _dataStore.GetAttendanceBySectionAndDateAsync(section.Id, request.Date, cancellationToken))
                .FirstOrDefault(r => r.StudentId == item.StudentId);

            if (existing != null)
            {
                existing.Status = item.Status;
                existing.Reason = item.Reason;
                existing.Remarks = item.Remarks;
                existing.RecordedByUserId = operatorUserId;
                await _dataStore.AddOrUpdateAttendanceRecordAsync(existing, cancellationToken);
            }
            else
            {
                var record = new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = item.StudentId,
                    SectionId = section.Id,
                    SchoolId = schoolId,
                    Date = request.Date,
                    Status = item.Status,
                    Reason = item.Reason,
                    Remarks = item.Remarks,
                    RecordedByUserId = operatorUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _dataStore.AddOrUpdateAttendanceRecordAsync(record, cancellationToken);
            }

            recordedCount++;

            // If marked absent or half-day, schedule for pattern check
            if (item.Status == AttendanceStatus.Absent || item.Status == AttendanceStatus.HalfDay)
            {
                atRiskStudentIds.Add(item.StudentId);
            }
        }

        // Run absence pattern detection in background/inline for at-risk students
        foreach (var studentId in atRiskStudentIds)
        {
            await _patternDetector.RunAbsencePatternAnalysisForStudentAsync(studentId, cancellationToken);
        }

        return ApiResponse<int>.Ok(recordedCount, $"Recorded attendance for {recordedCount} students.");
    }

    public async Task<ApiResponse<AttendanceRecordDto>> UpdateAttendanceRecordAsync(Guid recordId, UpdateAttendanceItemDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dataStore.GetAttendanceRecordByIdAsync(recordId, cancellationToken);
        if (record == null)
        {
            return ApiResponse<AttendanceRecordDto>.Fail("Attendance record not found.");
        }

        record.Status = request.Status;
        record.Reason = request.Reason;
        record.Remarks = request.Remarks;

        await _dataStore.AddOrUpdateAttendanceRecordAsync(record, cancellationToken);

        // Re-run pattern analysis
        await _patternDetector.RunAbsencePatternAnalysisForStudentAsync(record.StudentId, cancellationToken);

        var student = await _dataStore.GetStudentByIdAsync(record.StudentId, cancellationToken);
        var dto = new AttendanceRecordDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = student?.FullName ?? string.Empty,
            AdmissionNumber = student?.AdmissionNumber ?? string.Empty,
            SectionId = record.SectionId,
            Date = record.Date,
            Status = record.Status,
            Reason = record.Reason,
            Remarks = record.Remarks,
            RecordedByUserId = record.RecordedByUserId
        };

        return ApiResponse<AttendanceRecordDto>.Ok(dto, "Attendance updated successfully.");
    }
}
