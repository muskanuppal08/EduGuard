using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EduGuard.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly IEduGuardDataStore _dataStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IEduGuardDataStore dataStore, IPasswordHasher passwordHasher, ILogger<DataSeeder> logger)
    {
        _dataStore = dataStore;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting EduGuard data seeding...");

        // 1. Seed Roles
        var roleDefinitions = new (string Name, string Description)[]
        {
            (UserRoleType.SuperAdmin, "Full system access across all districts and schools"),
            (UserRoleType.DistrictAdmin, "District level education officer overseeing multiple schools"),
            (UserRoleType.SchoolPrincipal, "Head of school with operational and staff oversight"),
            (UserRoleType.Teacher, "Classroom teacher managing attendance and academic records"),
            (UserRoleType.Counselor, "Student counselor and social worker managing dropout interventions"),
            (UserRoleType.StudentParent, "Student and guardian read-only access to records")
        };

        var roleMap = new Dictionary<string, Role>();

        foreach (var (name, desc) in roleDefinitions)
        {
            var existing = await _dataStore.GetRoleByNameAsync(name, cancellationToken);
            if (existing == null)
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = desc,
                    IsSystemRole = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _dataStore.AddRoleAsync(role, cancellationToken);
                roleMap[name] = role;
                _logger.LogInformation("Seeded role: {RoleName}", name);
            }
            else
            {
                roleMap[name] = existing;
            }
        }

        // 2. Seed Permissions per Role
        var rolePermissions = new Dictionary<string, List<string>>
        {
            [UserRoleType.SuperAdmin] = SystemPermission.All.ToList(),
            [UserRoleType.DistrictAdmin] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.SchoolsWrite,
                SystemPermission.StudentsRead,
                SystemPermission.StudentsWrite,
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceAnalytics,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsReports,
                SystemPermission.DropoutAlertsRead,
                SystemPermission.DropoutRiskCalculate,
                SystemPermission.InterventionsManage
            },
            [UserRoleType.SchoolPrincipal] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.StudentsRead,
                SystemPermission.StudentsWrite,
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceRecord,
                SystemPermission.AttendanceAnalytics,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsRecord,
                SystemPermission.AcademicsReports,
                SystemPermission.DropoutAlertsRead,
                SystemPermission.DropoutRiskCalculate,
                SystemPermission.InterventionsManage
            },
            [UserRoleType.Teacher] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.StudentsRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceRecord,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsRecord,
                SystemPermission.DropoutAlertsRead
            },
            [UserRoleType.Counselor] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.StudentsRead,
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceAnalytics,
                SystemPermission.AcademicsRead,
                SystemPermission.DropoutAlertsRead,
                SystemPermission.DropoutRiskCalculate,
                SystemPermission.InterventionsManage
            },
            [UserRoleType.StudentParent] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AcademicsRead
            }
        };

        foreach (var (roleName, perms) in rolePermissions)
        {
            if (roleMap.TryGetValue(roleName, out var role))
            {
                await _dataStore.SetRolePermissionsAsync(role.Id, perms, cancellationToken);
            }
        }

        // 3. Seed Default SuperAdmin User
        const string adminUsername = "admin";
        const string adminEmail = "admin@eduguard.org";
        var existingAdmin = await _dataStore.GetUserByUsernameOrEmailAsync(adminUsername, cancellationToken);
        if (existingAdmin == null)
        {
            var (hash, salt) = _passwordHasher.HashPassword("AdminPassword123!");
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = adminUsername,
                Email = adminEmail,
                FullName = "System Administrator",
                PasswordHash = hash,
                PasswordSalt = salt,
                PhoneNumber = "+1234567890",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataStore.AddUserAsync(adminUser, cancellationToken);

            // Assign SuperAdmin role
            if (roleMap.TryGetValue(UserRoleType.SuperAdmin, out var superRole))
            {
                await _dataStore.AddUserRoleAsync(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = superRole.Id,
                    AssignedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            // Create Profile
            await _dataStore.UpsertProfileAsync(new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                Designation = "Chief System Administrator",
                Department = "Ministry / Central Administration",
                PreferredLanguage = "en",
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Default SuperAdmin user created: {Username} / {Email}", adminUsername, adminEmail);
        }

        // 4. Seed Sample School (Marginalized / Rural Focus)
        var sampleSchool = await _dataStore.GetSchoolByCodeAsync("SCH-RUR-001", cancellationToken);
        if (sampleSchool == null)
        {
            sampleSchool = new School
            {
                Id = Guid.NewGuid(),
                SchoolCode = "SCH-RUR-001",
                Name = "Govt High School, Birmitrapur",
                District = "Sundargarh",
                BlockOrZone = "Kutra Block",
                AreaType = AreaType.Rural,
                IsMarginalizedArea = true,
                Address = "Village Birmitrapur, Sector 4, Sundargarh",
                ContactEmail = "birmitrapur.high@eduguard.org",
                ContactPhone = "+919876543210",
                PrincipalName = "Dr. Manas Ranjan Dash",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddSchoolAsync(sampleSchool, cancellationToken);
            _logger.LogInformation("Seeded sample school: {SchoolName}", sampleSchool.Name);
        }

        // 5. Seed Academic Year
        var academicYear = await _dataStore.GetCurrentAcademicYearAsync(cancellationToken);
        if (academicYear == null)
        {
            academicYear = new AcademicYear
            {
                Id = Guid.NewGuid(),
                Name = "2025-2026",
                StartDate = new DateOnly(2025, 4, 1),
                EndDate = new DateOnly(2026, 3, 31),
                IsCurrent = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddAcademicYearAsync(academicYear, cancellationToken);
        }

        // 6. Seed Grade 9 & Section 9-A
        var grade9 = (await _dataStore.GetGradesBySchoolAsync(sampleSchool.Id, cancellationToken))
            .FirstOrDefault(g => g.GradeLevel == 9);

        if (grade9 == null)
        {
            grade9 = new ClassGrade
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                GradeLevel = 9,
                Name = "Grade 9",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddGradeAsync(grade9, cancellationToken);
        }

        var section9A = (await _dataStore.GetSectionsByGradeAsync(grade9.Id, academicYear.Id, cancellationToken))
            .FirstOrDefault(s => s.SectionName == "A");

        if (section9A == null)
        {
            section9A = new Section
            {
                Id = Guid.NewGuid(),
                ClassGradeId = grade9.Id,
                AcademicYearId = academicYear.Id,
                SectionName = "A",
                RoomNumber = "Room 101",
                Capacity = 45,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddSectionAsync(section9A, cancellationToken);
        }

        // 7. Seed Sample Students Demonstrating Dropout Risk Scenarios
        var existingStudent = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-001", cancellationToken);
        if (existingStudent == null)
        {
            // Student 1: Low risk
            var student1 = new Student
            {
                Id = Guid.NewGuid(),
                AdmissionNumber = "ADM-2025-001",
                FirstName = "Aarav",
                LastName = "Sharma",
                DateOfBirth = new DateOnly(2010, 5, 14),
                Gender = "Male",
                MotherTongue = "Odia",
                SocialCategory = "General",
                IsBPL = false,
                IsFirstGenerationLearner = false,
                DistanceToSchoolKm = 1.2m,
                TransportMode = TransportMode.Walking,
                FamilyIncomeTier = FamilyIncomeTier.Middle,
                GuardianName = "Rajesh Sharma",
                GuardianRelationship = "Father",
                GuardianPhone = "+919876543201",
                GuardianOccupation = "Teacher",
                SchoolId = sampleSchool.Id,
                CurrentSectionId = section9A.Id,
                CurrentStatus = StudentStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddStudentAsync(student1, cancellationToken);
            await _dataStore.AddEnrollmentAsync(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student1.Id,
                SectionId = section9A.Id,
                AcademicYearId = academicYear.Id,
                EnrollmentDate = new DateOnly(2025, 4, 5),
                Status = StudentStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Student 2: High Dropout Risk (Vulnerable marginalized demographic)
            var student2 = new Student
            {
                Id = Guid.NewGuid(),
                AdmissionNumber = "ADM-2025-002",
                FirstName = "Sunita",
                LastName = "Munda",
                DateOfBirth = new DateOnly(2010, 11, 20),
                Gender = "Female",
                MotherTongue = "Santhali",
                SocialCategory = "Scheduled Tribe",
                IsBPL = true,
                IsFirstGenerationLearner = true,
                IsSingleParentOrOrphan = true,
                DistanceToSchoolKm = 7.5m,
                TransportMode = TransportMode.Walking,
                FamilyIncomeTier = FamilyIncomeTier.BelowPovertyLine,
                GuardianName = "Phoolmani Munda",
                GuardianRelationship = "Mother",
                GuardianPhone = "+919876543202",
                GuardianOccupation = "Daily Wage Agricultural Labor",
                SchoolId = sampleSchool.Id,
                CurrentSectionId = section9A.Id,
                CurrentStatus = StudentStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddStudentAsync(student2, cancellationToken);
            await _dataStore.AddEnrollmentAsync(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student2.Id,
                SectionId = section9A.Id,
                AcademicYearId = academicYear.Id,
                EnrollmentDate = new DateOnly(2025, 4, 5),
                Status = StudentStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);


            _logger.LogInformation("Seeded sample school, classes, and students for {SchoolName}", sampleSchool.Name);
        }

        _logger.LogInformation("EduGuard data seeding completed successfully.");
    }
}
