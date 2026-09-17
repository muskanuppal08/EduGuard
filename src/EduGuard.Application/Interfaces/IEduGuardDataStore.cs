using EduGuard.Domain.Entities;

namespace EduGuard.Application.Interfaces;

public interface IEduGuardDataStore
{
    // Users
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);
    Task<List<User>> QueryUsersAsync(Func<User, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountUsersAsync(Func<User, bool> predicate, CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    // Roles
    Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);

    // User Roles
    Task<List<string>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    // Role Permissions
    Task<List<string>> GetPermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<List<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    // Refresh Tokens
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    // User Profile
    Task<UserProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpsertProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);

    // Schools
    Task<School?> GetSchoolByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<School?> GetSchoolByCodeAsync(string schoolCode, CancellationToken cancellationToken = default);
    Task<List<School>> QuerySchoolsAsync(Func<School, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountSchoolsAsync(Func<School, bool> predicate, CancellationToken cancellationToken = default);
    Task AddSchoolAsync(School school, CancellationToken cancellationToken = default);
    Task UpdateSchoolAsync(School school, CancellationToken cancellationToken = default);

    // Academic Years
    Task<AcademicYear?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AcademicYear>> GetAllAcademicYearsAsync(CancellationToken cancellationToken = default);
    Task<AcademicYear?> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default);
    Task AddAcademicYearAsync(AcademicYear year, CancellationToken cancellationToken = default);

    // Class Grades
    Task<ClassGrade?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ClassGrade>> GetGradesBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task AddGradeAsync(ClassGrade grade, CancellationToken cancellationToken = default);

    // Sections
    Task<Section?> GetSectionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Section>> GetSectionsByGradeAsync(Guid gradeId, Guid? academicYearId = null, CancellationToken cancellationToken = default);
    Task AddSectionAsync(Section section, CancellationToken cancellationToken = default);
    Task UpdateSectionAsync(Section section, CancellationToken cancellationToken = default);

    // Students
    Task<Student?> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetStudentByAdmissionNumberAsync(string admissionNumber, CancellationToken cancellationToken = default);
    Task<List<Student>> QueryStudentsAsync(Func<Student, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountStudentsAsync(Func<Student, bool> predicate, CancellationToken cancellationToken = default);
    Task AddStudentAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default);

    // Enrollments
    Task<Enrollment?> GetEnrollmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetActiveEnrollmentForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<List<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<List<Enrollment>> GetEnrollmentsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default);
    Task AddEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task UpdateEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default);

    // Student History
    Task<List<StudentHistory>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task AddHistoryItemAsync(StudentHistory history, CancellationToken cancellationToken = default);
}
