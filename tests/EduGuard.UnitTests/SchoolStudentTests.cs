using EduGuard.Application.DTOs.Classes;
using EduGuard.Application.DTOs.Enrollments;
using EduGuard.Application.DTOs.History;
using EduGuard.Application.DTOs.Schools;
using EduGuard.Application.DTOs.Students;
using EduGuard.Domain.Enums;
using EduGuard.Infrastructure.Persistence;
using EduGuard.Infrastructure.Security;
using EduGuard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduGuard.UnitTests;

public class SchoolStudentTests
{
    private readonly InMemoryEduGuardDataStore _dataStore;
    private readonly SchoolService _schoolService;
    private readonly ClassService _classService;
    private readonly StudentService _studentService;
    private readonly EnrollmentService _enrollmentService;
    private readonly StudentHistoryService _historyService;

    public SchoolStudentTests()
    {
        _dataStore = new InMemoryEduGuardDataStore();
        var seeder = new DataSeeder(_dataStore, new PasswordHasher(), NullLogger<DataSeeder>.Instance);
        seeder.SeedAsync().GetAwaiter().GetResult();

        _schoolService = new SchoolService(_dataStore);
        _classService = new ClassService(_dataStore);
        _studentService = new StudentService(_dataStore);
        _enrollmentService = new EnrollmentService(_dataStore);
        _historyService = new StudentHistoryService(_dataStore);
    }

    [Fact]
    public async Task SchoolService_CreateAndRetrieveSchool_ShouldSucceed()
    {
        // Act
        var result = await _schoolService.CreateSchoolAsync(new CreateSchoolDto
        {
            SchoolCode = "SCH-TEST-100",
            Name = "Kutra Tribal Residential School",
            District = "Sundargarh",
            BlockOrZone = "Kutra Block",
            AreaType = AreaType.Rural,
            IsMarginalizedArea = true,
            PrincipalName = "Smt. Kalyani Patel"
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("SCH-TEST-100", result.Data.SchoolCode);
        Assert.True(result.Data.IsMarginalizedArea);

        // Verify retrieval
        var getResult = await _schoolService.GetSchoolByIdAsync(result.Data.Id);
        Assert.True(getResult.Success);
        Assert.Equal("Kutra Tribal Residential School", getResult.Data!.Name);
    }

    [Fact]
    public async Task StudentService_RegisterStudent_WithSocioeconomicVulnerability_ShouldSucceed()
    {
        // Arrange: Retrieve seeded school
        var schools = await _schoolService.GetSchoolsAsync(new SchoolFilterDto());
        var schoolId = schools.Data!.Items[0].Id;

        // Act: Register at-risk marginalized student
        var registerResult = await _studentService.RegisterStudentAsync(new RegisterStudentDto
        {
            AdmissionNumber = "ADM-2026-TEST",
            FirstName = "Kiran",
            LastName = "Oram",
            DateOfBirth = new DateOnly(2011, 2, 18),
            Gender = "Female",
            MotherTongue = "Kurukh",
            SocialCategory = "Scheduled Tribe",
            IsBPL = true,
            IsFirstGenerationLearner = true,
            IsSingleParentOrOrphan = true,
            DistanceToSchoolKm = 6.8m,
            TransportMode = TransportMode.Walking,
            FamilyIncomeTier = FamilyIncomeTier.BelowPovertyLine,
            GuardianName = "Budhram Oram",
            GuardianRelationship = "Grandfather",
            GuardianOccupation = "Subsistence Farming",
            SchoolId = schoolId
        });

        // Assert
        Assert.True(registerResult.Success);
        Assert.NotNull(registerResult.Data);
        Assert.True(registerResult.Data.IsBPL);
        Assert.True(registerResult.Data.IsFirstGenerationLearner);
        Assert.Equal(6.8m, registerResult.Data.DistanceToSchoolKm);
        Assert.Equal(StudentStatus.Active, registerResult.Data.CurrentStatus);

        // Verify student history was initialized
        var historyResult = await _historyService.GetStudentHistoryTimelineAsync(registerResult.Data.Id);
        Assert.True(historyResult.Success);
        Assert.NotEmpty(historyResult.Data!);
    }

    [Fact]
    public async Task EnrollmentService_EnrollAndPromoteStudent_ShouldUpdateClassAndHistory()
    {
        // Arrange
        var schools = await _schoolService.GetSchoolsAsync(new SchoolFilterDto());
        var schoolId = schools.Data!.Items[0].Id;

        var gradeResult = await _classService.CreateGradeAsync(new CreateGradeDto
        {
            SchoolId = schoolId,
            GradeLevel = 8,
            Name = "Grade 8"
        });

        var yearResult = await _classService.GetCurrentAcademicYearAsync();

        var secAResult = await _classService.CreateSectionAsync(new CreateSectionDto
        {
            ClassGradeId = gradeResult.Data!.Id,
            AcademicYearId = yearResult.Data!.Id,
            SectionName = "A"
        });

        var secBResult = await _classService.CreateSectionAsync(new CreateSectionDto
        {
            ClassGradeId = gradeResult.Data!.Id,
            AcademicYearId = yearResult.Data!.Id,
            SectionName = "B"
        });

        var studentResult = await _studentService.RegisterStudentAsync(new RegisterStudentDto
        {
            AdmissionNumber = "ADM-PROMOTE-01",
            FirstName = "Subhash",
            LastName = "Kisan",
            DateOfBirth = new DateOnly(2011, 8, 12),
            SchoolId = schoolId,
            InitialSectionId = secAResult.Data!.Id,
            AcademicYearId = yearResult.Data!.Id
        });

        // Act: Promote student to Section B
        var promoteResult = await _enrollmentService.BatchPromoteStudentsAsync(new BatchPromoteDto
        {
            FromSectionId = secAResult.Data!.Id,
            ToSectionId = secBResult.Data!.Id,
            NewAcademicYearId = yearResult.Data!.Id,
            StudentIds = new List<Guid> { studentResult.Data!.Id }
        });

        // Assert
        Assert.True(promoteResult.Success);
        Assert.Equal(1, promoteResult.Data);

        var updatedStudent = await _studentService.GetStudentByIdAsync(studentResult.Data!.Id);
        Assert.Equal(secBResult.Data!.Id, updatedStudent.Data!.CurrentSectionId);

        var timeline = await _historyService.GetStudentHistoryTimelineAsync(studentResult.Data!.Id);
        Assert.Contains(timeline.Data!, h => h.EventType == "Promotion");
    }

    [Fact]
    public async Task EnrollmentService_RecordDropout_And_ReEnroll_ShouldTransitionProperly()
    {
        // Arrange: Find a seeded student
        var students = await _studentService.GetStudentsAsync(new StudentFilterDto());
        var student = students.Data!.Items[0];

        // Act 1: Record student dropout due to seasonal migration & poverty
        var dropoutResult = await _enrollmentService.RecordDropoutAsync(new RecordDropoutDto
        {
            StudentId = student.Id,
            DropoutDate = new DateOnly(2025, 9, 10),
            Reason = DropoutReason.SeasonalMigration,
            Remarks = "Family migrated to brick kiln for seasonal labor; student discontinued schooling."
        });

        // Assert Dropout
        Assert.True(dropoutResult.Success);
        var droppedStudent = await _studentService.GetStudentByIdAsync(student.Id);
        Assert.Equal(StudentStatus.DroppedOut, droppedStudent.Data!.CurrentStatus);

        // Act 2: Retention drive brings student back - Re-Enroll
        var reEnrollResult = await _enrollmentService.ReEnrollStudentAsync(new ReEnrollDto
        {
            StudentId = student.Id,
            SectionId = student.CurrentSectionId!.Value,
            AcademicYearId = (await _classService.GetCurrentAcademicYearAsync()).Data!.Id,
            Remarks = "Brought back into schooling after counselor outreach & seasonal migration return."
        });

        // Assert Re-Enroll
        Assert.True(reEnrollResult.Success);
        var reEnrolledStudent = await _studentService.GetStudentByIdAsync(student.Id);
        Assert.Equal(StudentStatus.Active, reEnrolledStudent.Data!.CurrentStatus);

        // Verify history captured both Dropout and ReEnrollment
        var timeline = await _historyService.GetStudentHistoryTimelineAsync(student.Id);
        Assert.Contains(timeline.Data!, h => h.EventType == "Dropout");
        Assert.Contains(timeline.Data!, h => h.EventType == "ReEnrollment");
    }

    [Fact]
    public async Task StudentHistoryService_AddTimelineNote_ShouldAppendToHistory()
    {
        // Arrange
        var students = await _studentService.GetStudentsAsync(new StudentFilterDto());
        var student = students.Data!.Items[0];

        // Act
        var noteResult = await _historyService.AddTimelineNoteAsync(new AddTimelineNoteDto
        {
            StudentId = student.Id,
            Title = "Home Visit Report",
            Description = "Counselor visited home to consult parents regarding regular bus transport."
        });

        // Assert
        Assert.True(noteResult.Success);
        var timeline = await _historyService.GetStudentHistoryTimelineAsync(student.Id);
        Assert.Contains(timeline.Data!, h => h.Title == "Home Visit Report");
    }
}
