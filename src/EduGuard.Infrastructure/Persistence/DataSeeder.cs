using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduGuard.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly IEduGuardDataStore _dataStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;
    private readonly IConfiguration? _configuration;
    private readonly IHostEnvironment? _hostEnvironment;

    public DataSeeder(
        IEduGuardDataStore dataStore,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger,
        IConfiguration? configuration = null,
        IHostEnvironment? hostEnvironment = null)
    {
        _dataStore = dataStore;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
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
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceRecord,
                SystemPermission.AttendanceAnalytics,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsRecord,
                SystemPermission.AcademicsReports,
                SystemPermission.DropoutAlertsRead,
                SystemPermission.InterventionsManage
            },
            [UserRoleType.Counselor] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.StudentsRead,
                SystemPermission.StudentsWrite,
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AttendanceRecord,
                SystemPermission.AttendanceAnalytics,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsReports,
                SystemPermission.DropoutAlertsRead,
                SystemPermission.DropoutRiskCalculate,
                SystemPermission.InterventionsManage
            },
            [UserRoleType.StudentParent] = new()
            {
                SystemPermission.SchoolsRead,
                SystemPermission.StudentsRead,
                SystemPermission.StudentsHistoryRead,
                SystemPermission.AttendanceRead,
                SystemPermission.AcademicsRead,
                SystemPermission.AcademicsReports
            }
        };

        foreach (var (roleName, perms) in rolePermissions)
        {
            if (roleMap.TryGetValue(roleName, out var role))
            {
                await _dataStore.SetRolePermissionsAsync(role.Id, perms, cancellationToken);
            }
        }

        // 3. Seed development personas or a deployment administrator.
        if (_hostEnvironment != null && !_hostEnvironment.IsDevelopment())
        {
            var bootstrapPassword = _configuration?["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(bootstrapPassword) || bootstrapPassword.Length < 16)
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin:Password must be configured with at least 16 characters outside Development.");
            }

            var bootstrapUsername = _configuration?["BootstrapAdmin:Username"] ?? "admin";
            var bootstrapEmail = _configuration?["BootstrapAdmin:Email"] ?? "admin@eduguard.org";
            await SeedUserIfNotExistsAsync(
                bootstrapUsername,
                bootstrapEmail,
                bootstrapPassword,
                "System Administrator",
                UserRoleType.SuperAdmin,
                "Chief System Administrator",
                "Central Ministry & Districts",
                roleMap,
                cancellationToken);
            _logger.LogInformation("Production seeding completed with the configured administrator only.");
            return;
        }
        else
        {
            await SeedUserIfNotExistsAsync("admin", "admin@eduguard.org", "AdminPassword123!", "System Administrator", UserRoleType.SuperAdmin, "Chief System Administrator", "Central Ministry & Districts", roleMap, cancellationToken);
            await SeedUserIfNotExistsAsync("teacher", "teacher@eduguard.org", "TeacherPassword123!", "Sunil Sharma (Teacher)", UserRoleType.Teacher, "Class Teacher (Grade 9-A)", "Faculty of Sciences", roleMap, cancellationToken, "+919876541001");
            await SeedUserIfNotExistsAsync("teacher_rekha", "rekha.nair@eduguard.org", "TeacherPassword123!", "Rekha Nair", UserRoleType.Teacher, "Class Teacher", "Mathematics", roleMap, cancellationToken, "+919876541002");
            await SeedUserIfNotExistsAsync("teacher_meera", "meera.iyer@eduguard.org", "TeacherPassword123!", "Meera Iyer", UserRoleType.Teacher, "Class Teacher", "Languages", roleMap, cancellationToken, "+919876541003");
            await SeedUserIfNotExistsAsync("teacher_arjun", "arjun.sen@eduguard.org", "TeacherPassword123!", "Arjun Sen", UserRoleType.Teacher, "Class Teacher", "Science", roleMap, cancellationToken, "+919876541004");
            await SeedUserIfNotExistsAsync("teacher_farida", "farida.khan@eduguard.org", "TeacherPassword123!", "Farida Khan", UserRoleType.Teacher, "Class Teacher", "Social Sciences", roleMap, cancellationToken, "+919876541005");
            await SeedUserIfNotExistsAsync("counselor", "counselor@eduguard.org", "CounselorPassword123!", "Dr. Ananya Roy (Counselor)", UserRoleType.Counselor, "Student Retention & Welfare Counselor", "Student Welfare Cell", roleMap, cancellationToken);
            await SeedUserIfNotExistsAsync("student", "student@eduguard.org", "StudentPassword123!", "Aarav Sharma", UserRoleType.StudentParent, "Enrolled Student (Grade 9-A)", "Student Body", roleMap, cancellationToken);
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

        var leadTeacher = await _dataStore.GetUserByUsernameOrEmailAsync("teacher", cancellationToken);
        if (leadTeacher != null && section9A.ClassTeacherUserId != leadTeacher.Id)
        {
            leadTeacher.SchoolId = sampleSchool.Id;
            await _dataStore.UpdateUserAsync(leadTeacher, cancellationToken);
            section9A.ClassTeacherUserId = leadTeacher.Id;
            await _dataStore.UpdateSectionAsync(section9A, cancellationToken);
        }

        var additionalSections = new List<Section>();
        var sectionAssignments = new[]
        {
            (Grade: 8, Name: "A", Room: "Room 204", Teacher: "teacher_rekha"),
            (Grade: 8, Name: "B", Room: "Room 205", Teacher: "teacher_meera"),
            (Grade: 10, Name: "A", Room: "Room 301", Teacher: "teacher_arjun"),
            (Grade: 10, Name: "B", Room: "Room 302", Teacher: "teacher_farida")
        };
        foreach (var assignment in sectionAssignments)
        {
            var teacher = await _dataStore.GetUserByUsernameOrEmailAsync(assignment.Teacher, cancellationToken);
            if (teacher == null) continue;
            teacher.SchoolId = sampleSchool.Id;
            await _dataStore.UpdateUserAsync(teacher, cancellationToken);
            additionalSections.Add(await GetOrCreateSectionAsync(
                sampleSchool.Id, academicYear.Id, assignment.Grade, assignment.Name,
                assignment.Room, teacher.Id, cancellationToken));
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

            var studentAccount = await _dataStore.GetUserByUsernameOrEmailAsync("student", cancellationToken);
            if (studentAccount != null && studentAccount.LinkedStudentId != student1.Id)
            {
                studentAccount.LinkedStudentId = student1.Id;
                studentAccount.SchoolId = sampleSchool.Id;
                await _dataStore.UpdateUserAsync(studentAccount, cancellationToken);
            }

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

            var sampleProfiles = new[]
            {
                (First: "Aisha", Last: "Patnaik", Gender: "Female", Guardian: "Sujata Patnaik", Phone: "+919876543203", Occupation: "Tailor", Language: "Odia"),
                (First: "Kabir", Last: "Das", Gender: "Male", Guardian: "Pradeep Das", Phone: "+919876543204", Occupation: "Shopkeeper", Language: "Bengali"),
                (First: "Leela", Last: "Naik", Gender: "Female", Guardian: "Madhabi Naik", Phone: "+919876543205", Occupation: "Health worker", Language: "Odia"),
                (First: "Rohan", Last: "Behera", Gender: "Male", Guardian: "Sanjay Behera", Phone: "+919876543206", Occupation: "Mechanic", Language: "Odia"),
                (First: "Maya", Last: "Kisku", Gender: "Female", Guardian: "Bimal Kisku", Phone: "+919876543207", Occupation: "Farmer", Language: "Santali"),
                (First: "Ishaan", Last: "Rout", Gender: "Male", Guardian: "Lalita Rout", Phone: "+919876543208", Occupation: "Nurse", Language: "Odia"),
                (First: "Nandini", Last: "Sahu", Gender: "Female", Guardian: "Gopal Sahu", Phone: "+919876543209", Occupation: "Driver", Language: "Hindi"),
                (First: "Farhan", Last: "Ali", Gender: "Male", Guardian: "Shabana Ali", Phone: "+919876543210", Occupation: "Weaver", Language: "Urdu"),
                (First: "Tara", Last: "Pradhan", Gender: "Female", Guardian: "Kamal Pradhan", Phone: "+919876543211", Occupation: "Clerk", Language: "Odia"),
                (First: "Dev", Last: "Mohanty", Gender: "Male", Guardian: "Anita Mohanty", Phone: "+919876543212", Occupation: "Electrician", Language: "Odia"),
                (First: "Pooja", Last: "Toppo", Gender: "Female", Guardian: "Ramesh Toppo", Phone: "+919876543213", Occupation: "Daily wage worker", Language: "Kurukh"),
                (First: "Neel", Last: "Chatterjee", Gender: "Male", Guardian: "Mitali Chatterjee", Phone: "+919876543214", Occupation: "Teacher", Language: "Bengali")
            };
            var additionalStudents = new List<Student>();
            for (var index = 0; index < sampleProfiles.Length; index++)
            {
                var profile = sampleProfiles[index];
                var section = additionalSections[index % additionalSections.Count];
                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    AdmissionNumber = $"ADM-2025-{index + 3:000}",
                    FirstName = profile.First,
                    LastName = profile.Last,
                    DateOfBirth = new DateOnly(2010 + index % 2, 1 + index % 12, 4 + index % 20),
                    Gender = profile.Gender,
                    MotherTongue = profile.Language,
                    SocialCategory = index % 3 == 0 ? "Scheduled Tribe" : index % 3 == 1 ? "OBC" : "General",
                    IsBPL = index % 4 == 1,
                    IsFirstGenerationLearner = index % 3 == 0,
                    HasSpecialNeeds = index == 8,
                    IsSingleParentOrOrphan = index % 7 == 0,
                    DistanceToSchoolKm = 1.1m + index * 0.6m,
                    TransportMode = (TransportMode)(index % 4),
                    FamilyIncomeTier = index % 4 == 0 ? FamilyIncomeTier.Middle :
                        index % 4 == 1 ? FamilyIncomeTier.BelowPovertyLine :
                        index % 4 == 2 ? FamilyIncomeTier.Low : FamilyIncomeTier.LowerMiddle,
                    GuardianName = profile.Guardian,
                    GuardianRelationship = index % 2 == 0 ? "Mother" : "Father",
                    GuardianPhone = profile.Phone,
                    GuardianOccupation = profile.Occupation,
                    ResidentialAddress = $"Ward {index + 2}, Birmitrapur, Sundargarh",
                    SchoolId = sampleSchool.Id,
                    CurrentSectionId = section.Id,
                    CurrentStatus = StudentStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _dataStore.AddStudentAsync(student, cancellationToken);
                await _dataStore.AddEnrollmentAsync(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    SectionId = section.Id,
                    AcademicYearId = academicYear.Id,
                    EnrollmentDate = new DateOnly(2025, 4, 5),
                    Status = StudentStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
                additionalStudents.Add(student);
            }

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

            foreach (var (student, index) in additionalStudents.Select((student, index) => (student, index)))
            {
                for (var day = 0; day < 20; day++)
                {
                    var date = baseDate.AddDays(day);
                    if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                    var absent = (day + index * 2) % 11 == 0;
                    var late = !absent && (day + index) % 13 == 0;
                    await _dataStore.AddOrUpdateAttendanceRecordAsync(new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        SectionId = student.CurrentSectionId!.Value,
                        SchoolId = sampleSchool.Id,
                        Date = date,
                        Status = absent ? AttendanceStatus.Absent : late ? AttendanceStatus.Late : AttendanceStatus.Present,
                        Reason = absent ? AbsenceReason.IllnessOrHealth : AbsenceReason.None,
                        CreatedAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                }
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

            var additionalAssessments = new Dictionary<Guid, List<Assessment>>();
            foreach (var section in additionalSections)
            {
                var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                var sectionAssessments = new List<Assessment>();
                foreach (var assessmentSeed in new[]
                {
                    (Subject: mathSubject, Title: "Mathematics Midterm", Category: AssessmentCategory.Midterm, Date: new DateOnly(2025, 6, 15)),
                    (Subject: mathSubject, Title: "Mathematics Quarterly", Category: AssessmentCategory.Quarterly, Date: new DateOnly(2025, 8, 20)),
                    (Subject: scienceSubject, Title: "Science Midterm", Category: AssessmentCategory.Midterm, Date: new DateOnly(2025, 6, 16)),
                    (Subject: socialSubject, Title: "Social Studies Midterm", Category: AssessmentCategory.Midterm, Date: new DateOnly(2025, 6, 18))
                })
                {
                    var assessment = new Assessment
                    {
                        Id = Guid.NewGuid(),
                        SchoolId = sampleSchool.Id,
                        SectionId = section.Id,
                        SubjectId = assessmentSeed.Subject.Id,
                        AcademicYearId = academicYear.Id,
                        Title = $"{grade?.Name}-{section.SectionName} {assessmentSeed.Title}",
                        Category = assessmentSeed.Category,
                        ExamDate = assessmentSeed.Date,
                        MaxMarks = 100m,
                        PassingMarks = 35m,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await _dataStore.AddAssessmentAsync(assessment, cancellationToken);
                    sectionAssessments.Add(assessment);
                }
                additionalAssessments[section.Id] = sectionAssessments;
            }

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

            foreach (var (student, index) in additionalStudents.Select((student, index) => (student, index)))
            {
                if (!additionalAssessments.TryGetValue(student.CurrentSectionId!.Value, out var assessmentsForSection))
                    continue;
                var mathMarks = new[] { 48m + index % 8 * 5m, 42m + index % 9 * 5m };
                for (var assessmentIndex = 0; assessmentIndex < assessmentsForSection.Count; assessmentIndex++)
                {
                    var assessment = assessmentsForSection[assessmentIndex];
                    var score = assessmentIndex switch
                    {
                        0 => mathMarks[0],
                        1 => mathMarks[1],
                        2 => 46m + index % 10 * 4m,
                        _ => 52m + index % 8 * 4m
                    };
                    await _dataStore.AddOrUpdateMarkAsync(new StudentExamMark
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = assessment.Id,
                        StudentId = student.Id,
                        MarksObtained = score,
                        GradeLetter = score >= 80m ? "A" : score >= 65m ? "B" : score >= 50m ? "C" : score >= 35m ? "D" : "F",
                        GradePoint = Math.Round(score / 25m, 1),
                        IsPass = score >= assessment.PassingMarks,
                        CreatedAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                }
            }

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

    private async Task<Section> GetOrCreateSectionAsync(
        Guid schoolId,
        Guid academicYearId,
        int gradeLevel,
        string sectionName,
        string roomNumber,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        var grade = (await _dataStore.GetGradesBySchoolAsync(schoolId, cancellationToken))
            .FirstOrDefault(item => item.GradeLevel == gradeLevel);
        if (grade == null)
        {
            grade = new ClassGrade
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                GradeLevel = gradeLevel,
                Name = $"Grade {gradeLevel}",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddGradeAsync(grade, cancellationToken);
        }

        var section = (await _dataStore.GetSectionsByGradeAsync(grade.Id, academicYearId, cancellationToken))
            .FirstOrDefault(item => item.SectionName == sectionName);
        if (section == null)
        {
            section = new Section
            {
                Id = Guid.NewGuid(),
                ClassGradeId = grade.Id,
                AcademicYearId = academicYearId,
                SectionName = sectionName,
                ClassTeacherUserId = teacherUserId,
                RoomNumber = roomNumber,
                Capacity = 40,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dataStore.AddSectionAsync(section, cancellationToken);
        }
        else if (section.ClassTeacherUserId != teacherUserId)
        {
            section.ClassTeacherUserId = teacherUserId;
            await _dataStore.UpdateSectionAsync(section, cancellationToken);
        }

        return section;
    }

    private async Task SeedUserIfNotExistsAsync(
        string username,
        string email,
        string password,
        string fullName,
        string roleName,
        string designation,
        string department,
        Dictionary<string, Role> roleMap,
        CancellationToken cancellationToken,
        string? phoneNumber = null)
    {
        var existing = await _dataStore.GetUserByUsernameOrEmailAsync(username, cancellationToken);
        if (existing == null)
        {
            var (hash, salt) = _passwordHasher.HashPassword(password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                FullName = fullName,
                PasswordHash = hash,
                PasswordSalt = salt,
                PhoneNumber = phoneNumber ?? "+919876543210",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataStore.AddUserAsync(user, cancellationToken);

            if (roleMap.TryGetValue(roleName, out var role))
            {
                await _dataStore.AddUserRoleAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            await _dataStore.UpsertProfileAsync(new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Designation = designation,
                Department = department,
                PreferredLanguage = "en",
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Seeded default user: {Username} / {Email} with role {RoleName}", username, email, roleName);
        }
    }
}
