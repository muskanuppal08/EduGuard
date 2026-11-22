using System.Globalization;
using System.Text;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using EduGuard.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[Route("api/v1/analytics")]
[RequirePermission(SystemPermission.StudentsRead)]
public class DropoutAnalyticsController : BaseApiController
{
    private static readonly IReadOnlyDictionary<string, string> SupportCategories = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CommuteDistance"] = "Commute hardship",
        ["FinancialDistress"] = "Financial hardship",
        ["MedicalHealth"] = "Health and medical support",
        ["AcademicRemedial"] = "Academic support",
        ["SafetyConcern"] = "Safety concern",
        ["OtherHardship"] = "Other student support"
    };

    private readonly IStudentRiskService _riskService;
    private readonly IEduGuardDataStore _store;
    private readonly StudentAccessPolicy _accessPolicy;

    public DropoutAnalyticsController(IStudentRiskService riskService, IEduGuardDataStore store, StudentAccessPolicy accessPolicy)
    {
        _riskService = riskService;
        _store = store;
        _accessPolicy = accessPolicy;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData([FromQuery] string period = "month", CancellationToken cancellationToken = default)
    {
        var riskSummary = await _riskService.GetRiskSummaryAsync(cancellationToken);
        var students = await _store.QueryStudentsAsync(_ => true, 1, 10000, cancellationToken);
        var visibleStudentIds = await VisibleStudentIdsAsync(cancellationToken);
        if (visibleStudentIds != null)
            students = students.Where(student => visibleStudentIds.Contains(student.Id)).ToList();
        riskSummary = ScopeRiskSummary(riskSummary, visibleStudentIds);
        var attendanceRows = new List<AttendanceRecord>();
        var marks = new List<StudentExamMark>();

        foreach (var student in students)
        {
            attendanceRows.AddRange(await _store.GetAttendanceByStudentAsync(student.Id, null, null, cancellationToken));
            marks.AddRange(await _store.GetMarksByStudentAsync(student.Id, cancellationToken));
        }

        var days = period.Equals("week", StringComparison.OrdinalIgnoreCase) ? 7
            : period.Equals("year", StringComparison.OrdinalIgnoreCase) ? 365 : 30;
        var latestAttendanceDate = attendanceRows.Count == 0 ? (DateOnly?)null : attendanceRows.Max(item => item.Date);
        var cutoff = latestAttendanceDate?.AddDays(-days + 1);
        var attendanceTrend = attendanceRows
            .Where(item => !cutoff.HasValue || item.Date >= cutoff.Value)
            .GroupBy(item => item.Date)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Date = group.Key.ToString("yyyy-MM-dd"),
                Percentage = decimal.Round(group.Count(item => item.Status is AttendanceStatus.Present or AttendanceStatus.Late) * 100m / group.Count(), 1),
                Records = group.Count()
            })
            .ToList();

        var assessments = await _store.QueryAssessmentsAsync(_ => true, cancellationToken);
        var assessmentById = assessments.ToDictionary(item => item.Id);
        var coreSubjectIds = new HashSet<Guid>();
        foreach (var subjectId in assessments.Select(item => item.SubjectId).Distinct())
        {
            var subject = await _store.GetSubjectByIdAsync(subjectId, cancellationToken);
            if (subject?.IsCoreSubject == true) coreSubjectIds.Add(subjectId);
        }
        var academicBySubject = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mark in marks.Where(item => !item.IsAbsent))
        {
            if (!assessmentById.TryGetValue(mark.AssessmentId, out var assessment) || assessment.MaxMarks <= 0)
                continue;
            var subject = await _store.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);
            if (subject == null) continue;
            if (!academicBySubject.TryGetValue(subject.Name, out var values))
                academicBySubject[subject.Name] = values = new List<decimal>();
            values.Add(mark.MarksObtained * 100m / assessment.MaxMarks);
        }

        var academicPerformance = academicBySubject
            .OrderBy(item => item.Key)
            .Select(item => new { Subject = item.Key, Average = decimal.Round(item.Value.Average(), 1), Assessments = item.Value.Count })
            .ToList();

        var interventions = await _riskService.GetInterventionsAsync(cancellationToken);
        if (visibleStudentIds != null)
            interventions = interventions.Where(item => visibleStudentIds.Contains(item.StudentId)).ToList();
        var academicShockStudents = marks
            .Where(mark => !mark.IsAbsent && assessmentById.ContainsKey(mark.AssessmentId))
            .GroupBy(mark => (mark.StudentId, assessmentById[mark.AssessmentId].SubjectId))
            .Where(group =>
            {
                var sequence = group
                    .Select(mark => new
                    {
                        Assessment = assessmentById[mark.AssessmentId],
                        Percentage = assessmentById[mark.AssessmentId].MaxMarks > 0
                            ? mark.MarksObtained * 100m / assessmentById[mark.AssessmentId].MaxMarks
                            : 0
                    })
                    .OrderBy(item => item.Assessment.ExamDate)
                    .ToList();
                return sequence.Zip(sequence.Skip(1), (previous, current) => previous.Percentage - current.Percentage)
                    .Any(drop => drop >= 15);
            })
            .Select(group => group.Key.StudentId)
            .Distinct()
            .Count();
        var knownRiskHistory = Array.Empty<object>();
        return Ok(new
        {
            riskDistribution = new
            {
                critical = riskSummary.CriticalRisk,
                high = riskSummary.HighRisk,
                moderate = riskSummary.ModerateRisk,
                low = riskSummary.LowRisk,
                unassessed = riskSummary.Unassessed
            },
            attendanceTrend,
            academicPerformance,
            riskTrend = knownRiskHistory,
            riskTrendNote = "Risk history will appear after periodic snapshots are enabled.",
            interventions = new
            {
                open = interventions.Count(item => item.Status.Equals("Open", StringComparison.OrdinalIgnoreCase)),
                inProgress = interventions.Count(item => item.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase)),
                resolved = interventions.Count(item => item.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase)),
                total = interventions.Count
            },
            totals = new
            {
                students = riskSummary.TotalStudents,
                highRisk = riskSummary.HighRisk + riskSummary.CriticalRisk,
                riskCoverage = riskSummary.TotalStudents == 0 ? 0 :
                    decimal.Round(riskSummary.Students.Count(item => item.AttendanceDataAvailable || item.AcademicDataAvailable) * 100m / riskSummary.TotalStudents, 1),
                chronicAbsentees = riskSummary.Students.Count(item => item.AttendanceDataAvailable && item.AttendancePercentage < 85),
                academicShock = academicShockStudents,
                coreSubjectFailures = marks
                    .Where(mark => !mark.IsAbsent && !mark.IsPass && assessmentById.ContainsKey(mark.AssessmentId))
                    .Select(mark => (mark.StudentId, SubjectId: assessmentById[mark.AssessmentId].SubjectId))
                    .Where(item => coreSubjectIds.Contains(item.SubjectId))
                    .Distinct()
                    .Count(),
                averageRiskScore = riskSummary.AverageRiskScore
            }
        });
    }

    [HttpGet("risk-summary")]
    public async Task<IActionResult> GetRiskSummary(CancellationToken cancellationToken)
    {
        var summary = await _riskService.GetRiskSummaryAsync(cancellationToken);
        return Ok(ScopeRiskSummary(summary, await VisibleStudentIdsAsync(cancellationToken)));
    }

    [HttpGet("risk/student/{studentId:guid}")]
    public async Task<IActionResult> GetStudentRisk(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();
        var result = await _riskService.GetStudentRiskAsync(studentId, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("interventions")]
    [RequirePermission(SystemPermission.InterventionsManage)]
    public async Task<IActionResult> GetInterventions(CancellationToken cancellationToken)
    {
        var items = await _riskService.GetInterventionsAsync(cancellationToken);
        var visibleStudentIds = await VisibleStudentIdsAsync(cancellationToken);
        return Ok(visibleStudentIds == null ? items : items.Where(item => visibleStudentIds.Contains(item.StudentId)).ToList());
    }

    [HttpPost("interventions")]
    [RequirePermission(SystemPermission.InterventionsManage)]
    public async Task<IActionResult> CreateIntervention([FromBody] CreateInterventionDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Plan) || string.IsNullOrWhiteSpace(request.AssignedTo))
            return BadRequest(new { message = "Plan and AssignedTo are required." });
        if (!await CanAccessStudentAsync(request.StudentId, cancellationToken))
            return Forbid();
        var item = await _riskService.CreateInterventionAsync(request, CurrentUserId, cancellationToken);
        return Ok(item);
    }

    [HttpPut("interventions/{id:guid}")]
    [RequirePermission(SystemPermission.InterventionsManage)]
    public async Task<IActionResult> UpdateIntervention(Guid id, [FromBody] UpdateInterventionDto request, CancellationToken cancellationToken)
    {
        var interventions = await _riskService.GetInterventionsAsync(cancellationToken);
        var existing = interventions.FirstOrDefault(item => item.Id == id);
        if (existing == null) return NotFound();
        if (!await CanAccessStudentAsync(existing.StudentId, cancellationToken))
            return Forbid();
        var item = await _riskService.UpdateInterventionAsync(id, request, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("notifications")]
    [RequirePermission(SystemPermission.UsersManage)]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
        => Ok(await _riskService.GetNotificationsAsync(cancellationToken));

    [HttpGet("helpdesk-requests")]
    [RequirePermission(SystemPermission.AttendanceRead)]
    public async Task<IActionResult> GetSupportRequests(CancellationToken cancellationToken)
    {
        var items = await _riskService.GetSupportRequestsAsync(cancellationToken);
        var visibleStudentIds = await VisibleStudentIdsAsync(cancellationToken);
        return Ok(visibleStudentIds == null ? items : items.Where(item => visibleStudentIds.Contains(item.StudentId)).ToList());
    }

    [HttpPost("helpdesk-requests")]
    [RequirePermission(SystemPermission.AttendanceRead)]
    public async Task<IActionResult> CreateSupportRequest([FromBody] CreateSupportRequestDto request, CancellationToken cancellationToken)
    {
        if (request.StudentId == Guid.Empty || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "A registered student and description are required." });
        if (!SupportCategories.TryGetValue(request.Category, out var categoryLabel))
            return BadRequest(new { message = "Select a supported request category." });
        request.Description = request.Description.Trim();
        if (request.Description.Length > 4000)
            return BadRequest(new { message = "Request descriptions must be 4,000 characters or fewer." });
        request.CategoryLabel = categoryLabel;
        if (request.Urgency is not ("Standard" or "High"))
            return BadRequest(new { message = "Urgency must be Standard or High." });
        if (!await CanAccessStudentAsync(request.StudentId, cancellationToken))
            return Forbid();
        request.ReportedBy = string.IsNullOrWhiteSpace(CurrentUsername) ? "EduGuard user" : CurrentUsername;
        var item = await _riskService.CreateSupportRequestAsync(request, cancellationToken);
        return item == null ? NotFound(new { message = "The selected student was not found." }) : Ok(item);
    }

    [HttpPut("helpdesk-requests/{id:guid}")]
    [RequirePermission(SystemPermission.InterventionsManage)]
    public async Task<IActionResult> UpdateSupportRequest(Guid id, [FromBody] UpdateSupportRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Status is not ("Under Review" or "In Progress" or "Resolved"))
            return BadRequest(new { message = "Unsupported request status." });
        request.ResolutionNotes = request.ResolutionNotes.Trim();
        if (request.ResolutionNotes.Length > 2000)
            return BadRequest(new { message = "Resolution notes must be 2,000 characters or fewer." });
        var requests = await _riskService.GetSupportRequestsAsync(cancellationToken);
        var existing = requests.FirstOrDefault(item => item.Id == id);
        if (existing == null) return NotFound();
        if (!await CanAccessStudentAsync(existing.StudentId, cancellationToken))
            return Forbid();
        var item = await _riskService.UpdateSupportRequestAsync(id, request, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost("notifications")]
    [RequirePermission(SystemPermission.UsersManage)]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Title and Message are required." });
        var item = await _riskService.CreateNotificationAsync(request, cancellationToken);
        return Ok(item);
    }

    [HttpPost("model/train")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    public async Task<IActionResult> TrainModel(CancellationToken cancellationToken)
        => Ok(await _riskService.TrainModelAsync(cancellationToken));

    [HttpGet("model/status")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    public async Task<IActionResult> GetModelStatus(CancellationToken cancellationToken)
    {
        var status = await _riskService.GetModelStatusAsync(cancellationToken);
        return status == null ? Ok(new { status = "Not trained", trainingRecords = 0 }) : Ok(status);
    }

    [HttpGet("reports/risk.csv")]
    [RequirePermission(SystemPermission.StudentsRead)]
    public async Task<IActionResult> DownloadRiskExcelCsv(CancellationToken cancellationToken)
    {
        var summary = await _riskService.GetRiskSummaryAsync(cancellationToken);
        summary = ScopeRiskSummary(summary, await VisibleStudentIdsAsync(cancellationToken));
        var csv = new StringBuilder("Admission Number,Student,Class,Risk Level,Risk Score,Attendance %,Academic Average,Factors\n");
        foreach (var student in summary.Students)
        {
            csv.AppendJoin(',', new[]
            {
                Escape(student.AdmissionNumber), Escape(student.StudentName), Escape(student.ClassName),
                Escape(student.RiskLevel), student.RiskScore.ToString(CultureInfo.InvariantCulture),
                student.AttendancePercentage.ToString(CultureInfo.InvariantCulture),
                student.AcademicAverage.ToString(CultureInfo.InvariantCulture),
                Escape(string.Join("; ", student.Factors))
            });
            csv.Append('\n');
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "EduGuard-Risk-Report.csv");
    }

    [HttpGet("reports/risk.pdf")]
    [RequirePermission(SystemPermission.StudentsRead)]
    public async Task<IActionResult> DownloadRiskPdfPrintView(CancellationToken cancellationToken)
    {
        var summary = await _riskService.GetRiskSummaryAsync(cancellationToken);
        summary = ScopeRiskSummary(summary, await VisibleStudentIdsAsync(cancellationToken));
        var rows = string.Join("", summary.Students.Select(s =>
            $"<tr><td>{System.Net.WebUtility.HtmlEncode(s.StudentName)}</td><td>{System.Net.WebUtility.HtmlEncode(s.RiskLevel)}</td><td>{s.RiskScore}</td><td>{s.AttendancePercentage}%</td><td>{s.AcademicAverage}%</td></tr>"));
        var html = $"<!doctype html><html><head><meta charset=\"utf-8\"><title>EduGuard Risk Report</title><style>body{{font-family:Arial;padding:32px}}table{{width:100%;border-collapse:collapse}}th,td{{border:1px solid #ddd;padding:8px;text-align:left}}th{{background:#312e81;color:white}}@media print{{button{{display:none}}}}</style></head><body><button onclick=\"window.print()\">Print / Save as PDF</button><h1>EduGuard Student Risk Report</h1><p>Generated {DateTime.Now:dd MMM yyyy}</p><p>Total: {summary.TotalStudents} | Critical: {summary.CriticalRisk} | High: {summary.HighRisk} | Moderate: {summary.ModerateRisk}</p><table><thead><tr><th>Student</th><th>Risk</th><th>Score</th><th>Attendance</th><th>Academics</th></tr></thead><tbody>{rows}</tbody></table></body></html>";
        return Content(html, "text/html");
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }

    private Task<HashSet<Guid>?> VisibleStudentIdsAsync(CancellationToken cancellationToken)
        => _accessPolicy.GetVisibleStudentIdsAsync(CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);

    private Task<bool> CanAccessStudentAsync(Guid studentId, CancellationToken cancellationToken)
        => _accessPolicy.CanAccessStudentAsync(studentId, CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);

    private static DropoutAnalyticsSummaryDto ScopeRiskSummary(DropoutAnalyticsSummaryDto summary, HashSet<Guid>? visibleStudentIds)
    {
        if (visibleStudentIds == null) return summary;
        var students = summary.Students.Where(student => visibleStudentIds.Contains(student.StudentId)).ToList();
        var assessed = students.Where(student => student.RiskLevel != "Unassessed").ToList();
        return new DropoutAnalyticsSummaryDto
        {
            TotalStudents = students.Count,
            CriticalRisk = students.Count(student => student.RiskLevel == "Critical"),
            HighRisk = students.Count(student => student.RiskLevel == "High"),
            ModerateRisk = students.Count(student => student.RiskLevel == "Moderate"),
            LowRisk = students.Count(student => student.RiskLevel == "Low"),
            Unassessed = students.Count(student => student.RiskLevel == "Unassessed"),
            AverageRiskScore = assessed.Count == 0 ? 0 : decimal.Round(assessed.Average(student => student.RiskScore), 1),
            Students = students
        };
    }
}
