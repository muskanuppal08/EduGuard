using EduGuard.Application.DTOs.Attendance;
using EduGuard.Domain.Enums;
using EduGuard.Infrastructure.Persistence;
using EduGuard.Infrastructure.Security;
using EduGuard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduGuard.UnitTests;

public class AttendanceTests
{
    private readonly InMemoryEduGuardDataStore _dataStore;
    private readonly AttendanceService _attendanceService;
    private readonly AttendanceAnalyticsService _analyticsService;
    private readonly AbsencePatternDetector _patternDetector;
    private readonly StudentHistoryService _historyService;

    public AttendanceTests()
    {
        _dataStore = new InMemoryEduGuardDataStore();
        var seeder = new DataSeeder(_dataStore, new PasswordHasher(), NullLogger<DataSeeder>.Instance);
        seeder.SeedAsync().GetAwaiter().GetResult();

        _patternDetector = new AbsencePatternDetector(_dataStore);
        _attendanceService = new AttendanceService(_dataStore, _patternDetector);
        _analyticsService = new AttendanceAnalyticsService(_dataStore);
        _historyService = new StudentHistoryService(_dataStore);
    }

    [Fact]
    public async Task AttendanceService_GetRosterAndRecordBatch_ShouldUpdateCounts()
    {
        // Arrange: Find seeded section
        var student1 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-001");
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student1);
        Assert.NotNull(student2);

        var sectionId = student1.CurrentSectionId!.Value;
        var testDate = new DateOnly(2025, 5, 2);

        // Act: Record batch attendance
        var batchResult = await _attendanceService.RecordDailyAttendanceBatchAsync(new RecordDailyAttendanceBatchDto
        {
            SectionId = sectionId,
            Date = testDate,
            Records = new List<RecordAttendanceItemDto>
            {
                new() { StudentId = student1.Id, Status = AttendanceStatus.Present },
                new() { StudentId = student2.Id, Status = AttendanceStatus.Absent, Reason = AbsenceReason.IllnessOrHealth, Remarks = "High fever reported by guardian" }
            }
        });

        // Assert
        Assert.True(batchResult.Success);
        Assert.Equal(2, batchResult.Data);

        // Verify roster reflects the marked state
        var rosterResult = await _attendanceService.GetAttendanceRosterAsync(sectionId, testDate);
        Assert.True(rosterResult.Success);
        Assert.Equal(1, rosterResult.Data!.PresentCount);
        Assert.Equal(1, rosterResult.Data.AbsentCount);

        var s2RosterItem = rosterResult.Data.Students.FirstOrDefault(s => s.StudentId == student2.Id);
        Assert.NotNull(s2RosterItem);
        Assert.Equal(AttendanceStatus.Absent, s2RosterItem.Status);
        Assert.Equal(AbsenceReason.IllnessOrHealth, s2RosterItem.Reason);
    }

    [Fact]
    public async Task AttendanceAnalytics_Summary_ShouldCalculatePercentagesAccurately()
    {
        // Arrange: Check seeded Student 1 (Aarav, regular)
        var student1 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-001");
        Assert.NotNull(student1);

        // Act
        var summaryResult = await _analyticsService.GetStudentAttendanceSummaryAsync(student1.Id);

        // Assert
        Assert.True(summaryResult.Success);
        Assert.NotNull(summaryResult.Data);
        Assert.True(summaryResult.Data.AttendancePercentage >= 90.0m);
        Assert.False(summaryResult.Data.IsChronicallyAbsent);
        Assert.Equal("Good", summaryResult.Data.AttendanceHealthTier);
    }

    [Fact]
    public async Task AbsencePatternDetector_DetectChronicAbsentees_ShouldIdentifyMarginalizedAtRiskStudent()
    {
        // Arrange: Student 2 (Sunita) was seeded with ~70% attendance due to seasonal farm work
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student2);

        // Act
        var chronicResult = await _patternDetector.DetectChronicAbsenteesAsync(student2.SchoolId, 85.0m);

        // Assert
        Assert.True(chronicResult.Success);
        Assert.NotNull(chronicResult.Data);
        Assert.Contains(chronicResult.Data, s => s.StudentId == student2.Id);

        var sunita = chronicResult.Data.First(s => s.StudentId == student2.Id);
        Assert.True(sunita.AttendancePercentage < 85.0m);
        Assert.True(sunita.IsBPL);
    }

    [Fact]
    public async Task AbsencePatternDetector_RunAnalysis_ShouldTriggerConsecutiveStreakAlert()
    {
        // Arrange: Student 2 has 4 consecutive absences seeded at the end
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student2);

        // Act
        var alertsResult = await _patternDetector.RunAbsencePatternAnalysisForStudentAsync(student2.Id);

        // Assert
        Assert.True(alertsResult.Success);
        Assert.NotEmpty(alertsResult.Data!);
        Assert.Contains(alertsResult.Data!, a => a.PatternType == AbsencePatternType.ConsecutiveAbsenceStreak);

        // Verify active alerts query
        var schoolAlerts = await _patternDetector.GetActiveAlertsBySchoolAsync(student2.SchoolId);
        Assert.True(schoolAlerts.Success);
        Assert.Contains(schoolAlerts.Data!, a => a.StudentId == student2.Id);
    }

    [Fact]
    public async Task AbsencePatternDetector_ResolveAlert_ShouldUpdateStatusAndLogTimeline()
    {
        // Arrange: Trigger alert
        var student2 = await _dataStore.GetStudentByAdmissionNumberAsync("ADM-2025-002");
        Assert.NotNull(student2);

        var alertsResult = await _patternDetector.RunAbsencePatternAnalysisForStudentAsync(student2.Id);
        var alert = alertsResult.Data!.First();

        // Act: Counselor resolves alert with intervention plan
        var resolveResult = await _patternDetector.ResolveAlertAsync(new ResolveAlertDto
        {
            AlertId = alert.Id,
            ResolutionNotes = "Counselor contacted mother; arranged study materials and flexible timing during harvest season."
        });

        // Assert
        Assert.True(resolveResult.Success);

        // Verify alert is marked resolved
        var activeAlerts = await _patternDetector.GetActiveAlertsBySchoolAsync(student2.SchoolId);
        Assert.DoesNotContain(activeAlerts.Data!, a => a.Id == alert.Id);

        // Verify logged in student timeline
        var timeline = await _historyService.GetStudentHistoryTimelineAsync(student2.Id);
        Assert.Contains(timeline.Data!, h => h.EventType == "AlertResolved");
    }
}
