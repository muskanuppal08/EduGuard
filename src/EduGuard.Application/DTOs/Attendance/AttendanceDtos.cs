using EduGuard.Domain.Enums;

namespace EduGuard.Application.DTOs.Attendance;

public class RecordAttendanceItemDto
{
    public Guid StudentId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public AbsenceReason Reason { get; set; } = AbsenceReason.None;
    public string? Remarks { get; set; }
}

public class RecordDailyAttendanceBatchDto
{
    public Guid SectionId { get; set; }
    public DateOnly Date { get; set; }
    public List<RecordAttendanceItemDto> Records { get; set; } = new();
}

public class UpdateAttendanceItemDto
{
    public AttendanceStatus Status { get; set; }
    public AbsenceReason Reason { get; set; }
    public string? Remarks { get; set; }
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public AbsenceReason Reason { get; set; }
    public string? Remarks { get; set; }
    public Guid? RecordedByUserId { get; set; }
}

public class StudentRosterAttendanceItemDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? AttendanceRecordId { get; set; }
    public AttendanceStatus? Status { get; set; }
    public AbsenceReason? Reason { get; set; }
    public string? Remarks { get; set; }
    public bool IsMarked => Status.HasValue;
}

public class AttendanceRosterDto
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    public int HalfDayCount { get; set; }
    public List<StudentRosterAttendanceItemDto> Students { get; set; } = new();
}
