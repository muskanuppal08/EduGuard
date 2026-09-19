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

            // Seed Attendance for Student 1 (Regular, ~95%)
            var baseDate = new DateOnly(2025, 4, 7);
            for (int i = 0; i < 20; i++)
            {
                var curDate = baseDate.AddDays(i);
                if (curDate.DayOfWeek == DayOfWeek.Saturday || curDate.DayOfWeek == DayOfWeek.Sunday) continue;

                await _dataStore.AddOrUpdateAttendanceRecordAsync(new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = student1.Id,
                    SectionId = section9A.Id,
                    SchoolId = sampleSchool.Id,
                    Date = curDate,
                    Status = (i == 5) ? AttendanceStatus.Late : AttendanceStatus.Present,
                    Reason = AbsenceReason.None,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            // Seed Attendance for Student 2 (Chronically Absent + 4 Consecutive Absences, ~70%)
            for (int i = 0; i < 20; i++)
            {
                var curDate = baseDate.AddDays(i);
                if (curDate.DayOfWeek == DayOfWeek.Saturday || curDate.DayOfWeek == DayOfWeek.Sunday) continue;

                // Mark absent on specific days and the last 4 consecutive days
                bool isAbsent = (i >= 16) || (i == 3) || (i == 8);
                var status = isAbsent ? AttendanceStatus.Absent : AttendanceStatus.Present;
                var reason = isAbsent ? AbsenceReason.AgriculturalOrSeasonalLabor : AbsenceReason.None;

                await _dataStore.AddOrUpdateAttendanceRecordAsync(new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = student2.Id,
                    SectionId = section9A.Id,
                    SchoolId = sampleSchool.Id,
                    Date = curDate,
                    Status = status,
                    Reason = reason,
                    Remarks = isAbsent ? "Assisting family during peak seasonal agricultural sowing" : null,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            _logger.LogInformation("Seeded sample students and attendance history for {SchoolName}", sampleSchool.Name);

            // ==================== Module 4: Seed Subjects, Assessments & Marks ====================
            var mathSubject = new Subject
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SubjectCode = "MATH-101",
                Name = "Mathematics",
                IsCoreSubject = true,
                Description = "Core Secondary School Mathematics",
                CreatedAtUtc = DateTime.UtcNow
            };
            var scienceSubject = new Subject
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SubjectCode = "SCI-101",
                Name = "General Science",
                IsCoreSubject = true,
                Description = "Core Secondary School Science",
                CreatedAtUtc = DateTime.UtcNow
            };
            var socialSubject = new Subject
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SubjectCode = "SOC-101",
                Name = "Social Studies",
                IsCoreSubject = false,
                Description = "General Social Studies & History",
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataStore.AddSubjectAsync(mathSubject, cancellationToken);
            await _dataStore.AddSubjectAsync(scienceSubject, cancellationToken);
            await _dataStore.AddSubjectAsync(socialSubject, cancellationToken);

            // Seed Assessments
            var mathMidterm = new Assessment
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SectionId = section9A.Id,
                SubjectId = mathSubject.Id,
                AcademicYearId = academicYear.Id,
                Title = "Class 9 Mathematics Midterm Exam",
                Category = AssessmentCategory.Midterm,
                ExamDate = new DateOnly(2025, 6, 15),
                MaxMarks = 100.0m,
                PassingMarks = 35.0m,
                CreatedAtUtc = DateTime.UtcNow
            };
            var mathQuarterly = new Assessment
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SectionId = section9A.Id,
                SubjectId = mathSubject.Id,
                AcademicYearId = academicYear.Id,
                Title = "Class 9 Mathematics Quarterly Assessment",
                Category = AssessmentCategory.Quarterly,
                ExamDate = new DateOnly(2025, 8, 20),
                MaxMarks = 100.0m,
                PassingMarks = 35.0m,
                CreatedAtUtc = DateTime.UtcNow
            };
            var scienceMidterm = new Assessment
            {
                Id = Guid.NewGuid(),
                SchoolId = sampleSchool.Id,
                SectionId = section9A.Id,
                SubjectId = scienceSubject.Id,
                AcademicYearId = academicYear.Id,
                Title = "Class 9 General Science Midterm Exam",
                Category = AssessmentCategory.Midterm,
                ExamDate = new DateOnly(2025, 6, 16),
                MaxMarks = 100.0m,
                PassingMarks = 35.0m,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataStore.AddAssessmentAsync(mathMidterm, cancellationToken);
            await _dataStore.AddAssessmentAsync(mathQuarterly, cancellationToken);
            await _dataStore.AddAssessmentAsync(scienceMidterm, cancellationToken);

            // Student 1 (Aarav): High performer across subjects
            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = mathMidterm.Id,
                StudentId = student1.Id,
                MarksObtained = 88.0m,
                GradeLetter = "A",
                GradePoint = 3.7m,
                IsPass = true,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = mathQuarterly.Id,
                StudentId = student1.Id,
                MarksObtained = 92.0m,
                GradeLetter = "A+",
                GradePoint = 4.0m,
                IsPass = true,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = scienceMidterm.Id,
                StudentId = student1.Id,
                MarksObtained = 85.0m,
                GradeLetter = "A",
                GradePoint = 3.7m,
                IsPass = true,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Student 2 (Sunita): Academic shock drop (40% -> 20%) and 2 core subject failures
            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = mathMidterm.Id,
                StudentId = student2.Id,
                MarksObtained = 40.0m,
                GradeLetter = "D",
                GradePoint = 1.0m,
                IsPass = true,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = mathQuarterly.Id,
                StudentId = student2.Id,
                MarksObtained = 20.0m, // 20% drop -> triggers Academic Shock & subject failure
                GradeLetter = "F",
                GradePoint = 0.0m,
                IsPass = false,
                Remarks = "Severe performance drop following prolonged agricultural absence",
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = scienceMidterm.Id,
                StudentId = student2.Id,
                MarksObtained = 28.0m, // Failed second core subject
                GradeLetter = "F",
                GradePoint = 0.0m,
                IsPass = false,
                Remarks = "Needs urgent academic remediation",
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Seeded sample academic subjects, assessments, and marks");
        }

        _logger.LogInformation("EduGuard data seeding completed successfully.");
    }
}
