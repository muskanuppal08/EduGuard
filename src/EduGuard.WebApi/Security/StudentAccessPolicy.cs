using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.Domain.Entities;

namespace EduGuard.WebApi.Security;

public sealed class StudentAccessPolicy
{
    private readonly IEduGuardDataStore _store;

    public StudentAccessPolicy(IEduGuardDataStore store)
    {
        _store = store;
    }

    public async Task<HashSet<Guid>?> GetVisibleStudentIdsAsync(
        Guid? userId,
        Guid? linkedStudentId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (roles.Any(IsAdministrator) || roles.Contains(UserRoleType.Counselor, StringComparer.OrdinalIgnoreCase))
            return null;

        if (roles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase))
        {
            if (userId.HasValue)
                linkedStudentId = (await _store.GetUserByIdAsync(userId.Value, cancellationToken))?.LinkedStudentId;
            return linkedStudentId.HasValue ? new HashSet<Guid> { linkedStudentId.Value } : new HashSet<Guid>();
        }

        if (roles.Contains(UserRoleType.Teacher, StringComparer.OrdinalIgnoreCase) && userId.HasValue)
        {
            var sectionIds = (await _store.GetSectionsByTeacherUserIdAsync(userId.Value, cancellationToken))
                .Select(section => section.Id)
                .ToHashSet();
            if (sectionIds.Count == 0) return new HashSet<Guid>();

            var students = await _store.QueryStudentsAsync(
                student => student.CurrentStatus == StudentStatus.Active &&
                           student.CurrentSectionId.HasValue && sectionIds.Contains(student.CurrentSectionId.Value),
                1,
                int.MaxValue,
                cancellationToken);
            return students.Select(student => student.Id).ToHashSet();
        }

        return new HashSet<Guid>();
    }

    public async Task<bool> CanAccessStudentAsync(
        Guid studentId,
        Guid? userId,
        Guid? linkedStudentId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        var visibleStudentIds = await GetVisibleStudentIdsAsync(userId, linkedStudentId, roles, cancellationToken);
        return visibleStudentIds == null || visibleStudentIds.Contains(studentId);
    }

    public async Task<bool> CanAccessSectionAsync(
        Guid sectionId,
        Guid? userId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (roles.Any(IsAdministrator) || roles.Contains(UserRoleType.Counselor, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!userId.HasValue || !roles.Contains(UserRoleType.Teacher, StringComparer.OrdinalIgnoreCase))
            return false;

        var section = await _store.GetSectionByIdAsync(sectionId, cancellationToken);
        return section?.ClassTeacherUserId == userId.Value;
    }

    public static bool IsAdministrator(string role)
        => role.Equals(UserRoleType.SuperAdmin, StringComparison.OrdinalIgnoreCase)
           || role.Equals(UserRoleType.DistrictAdmin, StringComparison.OrdinalIgnoreCase)
           || role.Equals(UserRoleType.SchoolPrincipal, StringComparison.OrdinalIgnoreCase);
}
