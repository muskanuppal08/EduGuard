namespace EduGuard.Domain.Enums;

public static class SystemPermission
{
    // User & Role Management
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";

    // School Management
    public const string SchoolsRead = "schools.read";
    public const string SchoolsWrite = "schools.write";

    // Student Management
    public const string StudentsRead = "students.read";
    public const string StudentsWrite = "students.write";
    public const string StudentsHistoryRead = "students.history.read";

    // Attendance Management
    public const string AttendanceRead = "attendance.read";
    public const string AttendanceRecord = "attendance.record";
    public const string AttendanceAnalytics = "attendance.analytics";

    // Academic Performance
    public const string AcademicsRead = "academics.read";
    public const string AcademicsRecord = "academics.record";
    public const string AcademicsReports = "academics.reports";

    // Dropout Risk & Interventions
    public const string DropoutAlertsRead = "dropout.alerts.read";
    public const string DropoutRiskCalculate = "dropout.risk.calculate";
    public const string InterventionsManage = "interventions.manage";

    public static readonly IReadOnlyList<string> All =
    [
        UsersManage,
        RolesManage,
        SchoolsRead,
        SchoolsWrite,
        StudentsRead,
        StudentsWrite,
        StudentsHistoryRead,
        AttendanceRead,
        AttendanceRecord,
        AttendanceAnalytics,
        AcademicsRead,
        AcademicsRecord,
        AcademicsReports,
        DropoutAlertsRead,
        DropoutRiskCalculate,
        InterventionsManage
    ];
}
