using System.Collections.Concurrent;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Persistence;

public class InMemoryEduGuardDataStore : IEduGuardDataStore
{
    // Module 1 stores
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ConcurrentDictionary<string, UserRole> _userRoles = new(); // key: userId:roleId
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _rolePermissions = new();
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new(); // key: tokenHash
    private readonly ConcurrentDictionary<Guid, UserProfile> _profiles = new(); // key: userId

    // Module 2 stores
    private readonly ConcurrentDictionary<Guid, School> _schools = new();
    private readonly ConcurrentDictionary<Guid, AcademicYear> _academicYears = new();
    private readonly ConcurrentDictionary<Guid, ClassGrade> _grades = new();
    private readonly ConcurrentDictionary<Guid, Section> _sections = new();
    private readonly ConcurrentDictionary<Guid, Student> _students = new();
    private readonly ConcurrentDictionary<Guid, Enrollment> _enrollments = new();
    private readonly ConcurrentDictionary<Guid, List<StudentHistory>> _studentHistories = new(); // key: studentId

    // Module 3 stores
    private readonly ConcurrentDictionary<string, AttendanceRecord> _attendance = new(); // key: studentId:yyyyMMdd
    private readonly ConcurrentDictionary<Guid, AbsenceAlert> _alerts = new();

    // Module 4 stores
    private readonly ConcurrentDictionary<Guid, Subject> _subjects = new();
    private readonly ConcurrentDictionary<Guid, Assessment> _assessments = new();
    private readonly ConcurrentDictionary<string, StudentExamMark> _marks = new(); // key: assessmentId:studentId

    // ==================== Users ====================
    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        var user = _users.Values.FirstOrDefault(u =>
            u.Username.ToLowerInvariant() == normalized ||
            u.Email.ToLowerInvariant() == normalized);
        return Task.FromResult(user);
    }

    public Task<List<User>> QueryUsersAsync(Func<User, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _users.Values
            .Where(predicate)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<int> CountUsersAsync(Func<User, bool> predicate, CancellationToken cancellationToken = default)
    {
        int count = _users.Values.Count(predicate);
        return Task.FromResult(count);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAtUtc = DateTime.UtcNow;
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    // ==================== Roles ====================
    public Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_roles.Values.OrderBy(r => r.Name).ToList());
    }

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var role = _roles.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(role);
    }

    public Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _roles.TryGetValue(id, out var role);
        return Task.FromResult(role);
    }

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[role.Id] = role;
        return Task.CompletedTask;
    }

    // ==================== User Roles ====================
    public Task<List<string>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = _userRoles.Values
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToHashSet();

        var roleNames = _roles.Values
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToList();

        return Task.FromResult(roleNames);
    }

    public Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        string key = $"{userRole.UserId}:{userRole.RoleId}";
        _userRoles[key] = userRole;
        return Task.CompletedTask;
    }

    public Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        string key = $"{userId}:{roleId}";
        _userRoles.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    // ==================== Role Permissions ====================
    public Task<List<string>> GetPermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        if (_rolePermissions.TryGetValue(roleId, out var perms))
        {
            return Task.FromResult(perms.OrderBy(p => p).ToList());
        }
        return Task.FromResult(new List<string>());
    }

    public Task<List<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = _userRoles.Values
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToList();

        var set = new HashSet<string>();
        foreach (var roleId in roleIds)
        {
            if (_rolePermissions.TryGetValue(roleId, out var perms))
            {
                foreach (var p in perms) set.Add(p);
            }
        }

        return Task.FromResult(set.OrderBy(p => p).ToList());
    }

    public Task SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        _rolePermissions[roleId] = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        return Task.CompletedTask;
    }

    // ==================== Refresh Tokens ====================
    public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _refreshTokens[token.TokenHash] = token;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        _refreshTokens.TryGetValue(tokenHash, out var token);
        return Task.FromResult(token);
    }

    public Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _refreshTokens[token.TokenHash] = token;
        return Task.CompletedTask;
    }

    public Task RevokeUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var token in _refreshTokens.Values.Where(t => t.UserId == userId && !t.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    // ==================== User Profile ====================
    public Task<UserProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(userId, out var profile);
        return Task.FromResult(profile);
    }

    public Task UpsertProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.UserId] = profile;
        return Task.CompletedTask;
    }

    // ==================== Schools ====================
    public Task<School?> GetSchoolByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _schools.TryGetValue(id, out var school);
        return Task.FromResult(school);
    }

    public Task<School?> GetSchoolByCodeAsync(string schoolCode, CancellationToken cancellationToken = default)
    {
        var school = _schools.Values.FirstOrDefault(s => s.SchoolCode.Equals(schoolCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(school);
    }

    public Task<List<School>> QuerySchoolsAsync(Func<School, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _schools.Values
            .Where(predicate)
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<int> CountSchoolsAsync(Func<School, bool> predicate, CancellationToken cancellationToken = default)
    {
        int count = _schools.Values.Count(predicate);
        return Task.FromResult(count);
    }

    public Task AddSchoolAsync(School school, CancellationToken cancellationToken = default)
    {
        _schools[school.Id] = school;
        return Task.CompletedTask;
    }

    public Task UpdateSchoolAsync(School school, CancellationToken cancellationToken = default)
    {
        school.UpdatedAtUtc = DateTime.UtcNow;
        _schools[school.Id] = school;
        return Task.CompletedTask;
    }

    // ==================== Academic Years ====================
    public Task<AcademicYear?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _academicYears.TryGetValue(id, out var year);
        return Task.FromResult(year);
    }

    public Task<List<AcademicYear>> GetAllAcademicYearsAsync(CancellationToken cancellationToken = default)
    {
        var items = _academicYears.Values.OrderByDescending(y => y.StartDate).ToList();
        return Task.FromResult(items);
    }

    public Task<AcademicYear?> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default)
    {
        var year = _academicYears.Values.FirstOrDefault(y => y.IsCurrent) ??
                   _academicYears.Values.OrderByDescending(y => y.StartDate).FirstOrDefault();
        return Task.FromResult(year);
    }

    public Task AddAcademicYearAsync(AcademicYear year, CancellationToken cancellationToken = default)
    {
        if (year.IsCurrent)
        {
            foreach (var y in _academicYears.Values)
            {
                y.IsCurrent = false;
            }
        }
        _academicYears[year.Id] = year;
        return Task.CompletedTask;
    }

    // ==================== Class Grades ====================
    public Task<ClassGrade?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _grades.TryGetValue(id, out var grade);
        return Task.FromResult(grade);
    }

    public Task<List<ClassGrade>> GetGradesBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var items = _grades.Values
            .Where(g => g.SchoolId == schoolId)
            .OrderBy(g => g.GradeLevel)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddGradeAsync(ClassGrade grade, CancellationToken cancellationToken = default)
    {
        _grades[grade.Id] = grade;
        return Task.CompletedTask;
    }

    // ==================== Sections ====================
    public Task<Section?> GetSectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sections.TryGetValue(id, out var section);
        return Task.FromResult(section);
    }

    public Task<List<Section>> GetSectionsByGradeAsync(Guid gradeId, Guid? academicYearId = null, CancellationToken cancellationToken = default)
    {
        var items = _sections.Values
            .Where(s => s.ClassGradeId == gradeId &&
                        (!academicYearId.HasValue || s.AcademicYearId == academicYearId.Value))
            .OrderBy(s => s.SectionName)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddSectionAsync(Section section, CancellationToken cancellationToken = default)
    {
        _sections[section.Id] = section;
        return Task.CompletedTask;
    }

    public Task UpdateSectionAsync(Section section, CancellationToken cancellationToken = default)
    {
        section.UpdatedAtUtc = DateTime.UtcNow;
        _sections[section.Id] = section;
        return Task.CompletedTask;
    }

    // ==================== Students ====================
    public Task<Student?> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _students.TryGetValue(id, out var student);
        return Task.FromResult(student);
    }

    public Task<Student?> GetStudentByAdmissionNumberAsync(string admissionNumber, CancellationToken cancellationToken = default)
    {
        var student = _students.Values.FirstOrDefault(s => s.AdmissionNumber.Equals(admissionNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(student);
    }

    public Task<List<Student>> QueryStudentsAsync(Func<Student, bool> predicate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _students.Values
            .Where(predicate)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<int> CountStudentsAsync(Func<Student, bool> predicate, CancellationToken cancellationToken = default)
    {
        int count = _students.Values.Count(predicate);
        return Task.FromResult(count);
    }

    public Task AddStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        _students[student.Id] = student;
        return Task.CompletedTask;
    }

    public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        student.UpdatedAtUtc = DateTime.UtcNow;
        _students[student.Id] = student;
        return Task.CompletedTask;
    }

    // ==================== Enrollments ====================
    public Task<Enrollment?> GetEnrollmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _enrollments.TryGetValue(id, out var enrollment);
        return Task.FromResult(enrollment);
    }

    public Task<Enrollment?> GetActiveEnrollmentForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var enrollment = _enrollments.Values
            .FirstOrDefault(e => e.StudentId == studentId && e.Status == Domain.Enums.StudentStatus.Active);
        return Task.FromResult(enrollment);
    }

    public Task<List<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var items = _enrollments.Values
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<Enrollment>> GetEnrollmentsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var items = _enrollments.Values
            .Where(e => e.SectionId == sectionId && e.Status == Domain.Enums.StudentStatus.Active)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _enrollments[enrollment.Id] = enrollment;
        return Task.CompletedTask;
    }

    public Task UpdateEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        _enrollments[enrollment.Id] = enrollment;
        return Task.CompletedTask;
    }

    // ==================== Student History ====================
    public Task<List<StudentHistory>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        if (_studentHistories.TryGetValue(studentId, out var list))
        {
            var sorted = list.OrderByDescending(h => h.EventDateUtc).ToList();
            return Task.FromResult(sorted);
        }
        return Task.FromResult(new List<StudentHistory>());
    }

    public Task AddHistoryItemAsync(StudentHistory history, CancellationToken cancellationToken = default)
    {
        _studentHistories.AddOrUpdate(
            history.StudentId,
            new List<StudentHistory> { history },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(history);
                }
                return existing;
            });
        return Task.CompletedTask;
    }

    // ==================== Attendance ====================
    public Task<AttendanceRecord?> GetAttendanceRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = _attendance.Values.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(record);
    }

    public Task<List<AttendanceRecord>> GetAttendanceByStudentAsync(Guid studentId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default)
    {
        var items = _attendance.Values
            .Where(a => a.StudentId == studentId &&
                        (!startDate.HasValue || a.Date >= startDate.Value) &&
                        (!endDate.HasValue || a.Date <= endDate.Value))
            .OrderBy(a => a.Date)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<AttendanceRecord>> GetAttendanceBySectionAndDateAsync(Guid sectionId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var items = _attendance.Values
            .Where(a => a.SectionId == sectionId && a.Date == date)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddOrUpdateAttendanceRecordAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        string key = $"{record.StudentId}:{record.Date:yyyyMMdd}";
        _attendance[key] = record;
        return Task.CompletedTask;
    }

    // ==================== Absence Alerts ====================
    public Task<AbsenceAlert?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _alerts.TryGetValue(id, out var alert);
        return Task.FromResult(alert);
    }

    public Task<List<AbsenceAlert>> GetAlertsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var items = _alerts.Values
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.TriggeredDateUtc)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<AbsenceAlert>> GetActiveAlertsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var items = _alerts.Values
            .Where(a => a.SchoolId == schoolId && !a.IsResolved)
            .OrderByDescending(a => a.TriggeredDateUtc)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddAbsenceAlertAsync(AbsenceAlert alert, CancellationToken cancellationToken = default)
    {
        _alerts[alert.Id] = alert;
        return Task.CompletedTask;
    }

    public Task UpdateAbsenceAlertAsync(AbsenceAlert alert, CancellationToken cancellationToken = default)
    {
        alert.UpdatedAtUtc = DateTime.UtcNow;
        _alerts[alert.Id] = alert;
        return Task.CompletedTask;
    }

    // ==================== Subjects ====================
    public Task<Subject?> GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _subjects.TryGetValue(id, out var subject);
        return Task.FromResult(subject);
    }

    public Task<List<Subject>> GetSubjectsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var items = _subjects.Values
            .Where(s => s.SchoolId == schoolId)
            .OrderBy(s => s.Name)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _subjects[subject.Id] = subject;
        return Task.CompletedTask;
    }

    // ==================== Assessments ====================
    public Task<Assessment?> GetAssessmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _assessments.TryGetValue(id, out var assessment);
        return Task.FromResult(assessment);
    }

    public Task<List<Assessment>> QueryAssessmentsAsync(Func<Assessment, bool> predicate, CancellationToken cancellationToken = default)
    {
        var items = _assessments.Values
            .Where(predicate)
            .OrderByDescending(a => a.ExamDate)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddAssessmentAsync(Assessment assessment, CancellationToken cancellationToken = default)
    {
        _assessments[assessment.Id] = assessment;
        return Task.CompletedTask;
    }

    // ==================== Student Exam Marks ====================
    public Task<StudentExamMark?> GetMarkByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mark = _marks.Values.FirstOrDefault(m => m.Id == id);
        return Task.FromResult(mark);
    }

    public Task<List<StudentExamMark>> GetMarksByAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var items = _marks.Values
            .Where(m => m.AssessmentId == assessmentId)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<StudentExamMark>> GetMarksByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var items = _marks.Values
            .Where(m => m.StudentId == studentId)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddOrUpdateMarkAsync(StudentExamMark mark, CancellationToken cancellationToken = default)
    {
        string key = $"{mark.AssessmentId}:{mark.StudentId}";
        _marks[key] = mark;
        return Task.CompletedTask;
    }
}
