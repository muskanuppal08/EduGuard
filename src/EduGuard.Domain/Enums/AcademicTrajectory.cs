namespace EduGuard.Domain.Enums;

public enum AcademicTrajectory
{
    Improving,
    Stable,
    Declining,
    AcademicShock  // Sharp drop >= 15% between consecutive evaluation periods
}
