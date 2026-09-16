namespace EduGuard.Domain.Enums;

public static class UserRoleType
{
    public const string SuperAdmin = "SuperAdmin";
    public const string DistrictAdmin = "DistrictAdmin";
    public const string SchoolPrincipal = "SchoolPrincipal";
    public const string Teacher = "Teacher";
    public const string Counselor = "Counselor";
    public const string StudentParent = "StudentParent";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        DistrictAdmin,
        SchoolPrincipal,
        Teacher,
        Counselor,
        StudentParent
    ];
}
