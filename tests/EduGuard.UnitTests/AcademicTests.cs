using EduGuard.Application.DTOs.Academics;
using EduGuard.Domain.Enums;
using EduGuard.Infrastructure.Persistence;
using EduGuard.Infrastructure.Security;
using EduGuard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduGuard.UnitTests;

public class AcademicTests
{
    private readonly InMemoryEduGuardDataStore _dataStore;
    private readonly AcademicService _academicService;
    private readonly MarksService _marksService;
    private readonly PerformanceAnalyticsService _analyticsService;

    public AcademicTests()
    {
        _dataStore = new InMemoryEduGuardDataStore();
        var seeder = new DataSeeder(_dataStore, new PasswordHasher(), NullLogger<DataSeeder>.Instance);
        seeder.SeedAsync().GetAwaiter().GetResult();

        _academicService = new AcademicService(_dataStore);
        _marksService = new MarksService(_dataStore);
        _analyticsService = new PerformanceAnalyticsService(_dataStore);
    }

    [Fact]
    public async Task AcademicService_CreateSubjectAndAssessment_ShouldSucceed()
    {
        // Arrange
        var schools = await _dataStore.QuerySchoolsAsync(_ => true, 1, 10);
        var school = Assert.Single(schools);

        // Act: Create Subject
        var createSubResult = await _academicService.CreateSubjectAsync(new CreateSubjectDto
        {
            SchoolId = school.Id,
            SubjectCode = "ENG-101",
            Name = "English Literature",
            IsCoreSubject = true,
            Description = "Language & Reading Comprehension"
        });

        Assert.True(createSubResult.Success);
        Assert.Equal("ENG-101", createSubResult.Data!.SubjectCode);
        Assert.True(createSubResult.Data.IsCoreSubject);

        // Act: Create Assessment
        var grades = await _dataStore.GetGradesBySchoolAsync(school.Id);
        var sections = await _dataStore.GetSectionsByGradeAsync(grades.First().Id);
        var section = sections.First();

        var createAssessmentResult = await _academicService.CreateAssessmentAsync(new CreateAssessmentDto
        {
            SchoolId = school.Id,
            SectionId = section.Id,
            SubjectId = createSubResult.Data.Id,
            AcademicYearId = section.AcademicYearId,
            Title = "English Term 1 Exam",
            Category = AssessmentCategory.Midterm,
            ExamDate = new DateOnly(2025, 7, 10),
            MaxMarks = 100,
            PassingMarks = 40
        });

        // Assert
        Assert.True(createAssessmentResult.Success);
        Assert.Equal("English Term 1 Exam", createAssessmentResult.Data!.Title);
        Assert.Equal(40, createAssessmentResult.Data.PassingMarks);
    }

    [Fact]
    public async Task MarksService_RecordMarksBatchAndGetRoster_ShouldCalculateGradesCorrectly()
    {
        // Arrange
        var student1 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-001");
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student1);
        Assert.NotNull(student2);

        var assessments = await _dataStore.QueryAssessmentsAsync(a => a.Title.Contains("Mathematics Midterm"));
        var assessment = Assert.Single(assessments);

        // Act: Record Marks
        var recordResult = await _marksService.RecordMarksBatchAsync(new RecordMarksBatchDto
        {
            AssessmentId = assessment.Id,
            Marks = new List<RecordStudentMarkItemDto>
            {
                new() { StudentId = student1.Id, MarksObtained = 95, IsAbsent = false, Remarks = "Excellent work" },
                new() { StudentId = student2.Id, MarksObtained = 0, IsAbsent = true, Remarks = "Unexcused absence" }
            }
        });

        Assert.True(recordResult.Success);
        Assert.Equal(2, recordResult.Data);

        // Assert Roster
        var rosterResult = await _marksService.GetExamRosterAsync(assessment.Id);
        Assert.True(rosterResult.Success);

        var student1Roster = rosterResult.Data!.Students.First(s => s.StudentId == student1.Id);
        Assert.Equal(95, student1Roster.MarksObtained);
        Assert.Equal("A+", student1Roster.GradeLetter);
        Assert.True(student1Roster.IsPass);

        var student2Roster = rosterResult.Data!.Students.First(s => s.StudentId == student2.Id);
        Assert.True(student2Roster.IsAbsent);
        Assert.Equal("F", student2Roster.GradeLetter);
        Assert.False(student2Roster.IsPass);
    }

    [Fact]
    public async Task PerformanceAnalytics_GenerateReportCard_ShouldIdentifyCoreSubjectFailures()
    {
        // Arrange: Seeded student 2 (Sunita) has failing marks in Math and Science (both core subjects)
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student2);

        // Act
        var reportCardResult = await _analyticsService.GenerateReportCardAsync(student2.Id);

        // Assert
        Assert.True(reportCardResult.Success);
        var reportCard = reportCardResult.Data!;

        Assert.True(reportCard.IsAtAcademicRisk);
        Assert.True(reportCard.CoreSubjectsFailedCount >= 2);
        Assert.Equal("Critical", reportCard.AcademicRiskSeverity);
    }

    [Fact]
    public async Task PerformanceAnalytics_GetAcademicTrajectory_ShouldDetectAcademicShock()
    {
        // Arrange: Seeded student 2 went from 65% in Math Midterm down to 25% in Math Quarterly
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student2);

        // Act
        var trajectoryResult = await _analyticsService.GetAcademicTrajectoryAsync(student2.Id);

        // Assert
        Assert.True(trajectoryResult.Success);
        var trajectory = trajectoryResult.Data!;

        Assert.Equal("AcademicShock", trajectory.Trajectory);
        Assert.True(trajectory.HasAcademicShock);
        Assert.True(trajectory.MaxDropPercentage >= 15.0m);
    }

    [Fact]
    public async Task PerformanceAnalytics_GetAtRiskAcademicStudentsBySchool_ShouldFlagHighRiskStudents()
    {
        // Arrange
        var schools = await _dataStore.QuerySchoolsAsync(_ => true, 1, 10);
        var school = Assert.Single(schools);

        // Act
        var atRiskResult = await _analyticsService.GetAtRiskAcademicStudentsBySchoolAsync(school.Id);

        // Assert
        Assert.True(atRiskResult.Success);
        Assert.NotEmpty(atRiskResult.Data!);

        var atRiskStudent = atRiskResult.Data!.First();
        Assert.Equal("ADM-2025-002", atRiskStudent.AdmissionNumber);
        Assert.True(atRiskStudent.IsAtAcademicRisk);
    }
}
