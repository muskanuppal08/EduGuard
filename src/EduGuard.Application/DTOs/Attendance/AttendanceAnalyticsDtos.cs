namespace EduGuard.Application.DTOs.Attendance;

public class StudentAttendanceSummaryDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int TotalWorkingDays { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysExcused { get; set; }
    public int DaysLate { get; set; }
    public int DaysHalfDay { get; set; }
    public decimal AttendancePercentage { get; set; }
    public decimal Rolling30DayPercentage { get; set; }
    public int ConsecutiveAbsentDays { get; set; }
    public bool IsChronicallyAbsent => AttendancePercentage < 85.0m;
    public string AttendanceHealthTier =>
        AttendancePercentage >= 90.0m ? "Good" :
        AttendancePercentage >= 85.0m ? "Warning" :
        AttendancePercentage >= 75.0m ? "ChronicallyAbsent" : "SevereAbsence";
}

public class AttendanceCalendarDayDto
{
    public DateOnly Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class StudentAttendanceCalendarDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public List<AttendanceCalendarDayDto> Days { get; set; } = new();
}

public class SectionAttendanceStatsDto
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int LateCount { get; set; }
    public int HalfDayCount { get; set; }
    public decimal DailyAttendanceRate { get; set; }
}

public class MonthlyAttendanceTrendPointDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal AttendanceRate { get; set; }
    public int TotalDays { get; set; }
}

public class AttendanceTrendDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public List<MonthlyAttendanceTrendPointDto> MonthlyPoints { get; set; } = new();
    public string Trajectory { get; set; } = "Stable"; // "Improving", "Stable", "Declining", "CriticalDrop"
}
