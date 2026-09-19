namespace EduGuard.Domain.Enums;

public enum AbsencePatternType
{
    ChronicAbsenteeism,         // Overall attendance < 85%
    ConsecutiveAbsenceStreak,   // 3 or more consecutive unexcused absences
    DayOfWeekPattern,           // Repeated absences on specific weekdays (e.g. Mondays/Fridays)
    SharpAttendanceDrop         // Attendance dropped by >= 10% compared to previous month
}
