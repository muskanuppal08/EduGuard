using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using EduGuard.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.AcademicsRead)]
public class AcademicsController : BaseApiController
{
    private readonly IAcademicService _academicService;
    private readonly IMarksService _marksService;
    private readonly IPerformanceAnalyticsService _analyticsService;
    private readonly IEduGuardDataStore _store;
    private readonly StudentAccessPolicy _accessPolicy;

    public AcademicsController(
        IAcademicService academicService,
        IMarksService marksService,
        IPerformanceAnalyticsService analyticsService,
        IEduGuardDataStore store,
        StudentAccessPolicy accessPolicy)
    {
        _academicService = academicService;
        _marksService = marksService;
        _analyticsService = analyticsService;
        _store = store;
        _accessPolicy = accessPolicy;
    }

    /// <summary>
    /// Creates a new academic subject for a school.
    /// </summary>
    [HttpPost("subjects")]
    [RequirePermission(SystemPermission.AcademicsRecord)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto request, CancellationToken cancellationToken)
    {
        var result = await _academicService.CreateSubjectAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all subjects configured for a given school.
    /// </summary>
    [HttpGet("subjects/school/{schoolId}")]
    [ProducesResponseType(typeof(ApiResponse<List<SubjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectsBySchool(Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _academicService.GetSubjectsBySchoolAsync(schoolId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves subject details by subject ID.
    /// </summary>
    [HttpGet("subjects/{id}")]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjectById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _academicService.GetSubjectByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Creates and schedules an assessment/examination.
    /// </summary>
    [HttpPost("assessments")]
    [RequirePermission(SystemPermission.AcademicsRecord)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentDto request, CancellationToken cancellationToken)
    {
        if (!await CanAccessSectionAsync(request.SectionId, cancellationToken))
            return Forbid();

        var result = await _academicService.CreateAssessmentAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Filters and queries scheduled assessments.
    /// </summary>
    [HttpGet("assessments")]
    [ProducesResponseType(typeof(ApiResponse<List<AssessmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssessments([FromQuery] AssessmentFilterDto filter, CancellationToken cancellationToken)
    {
        var accessibleSections = await AccessibleSectionIdsAsync(cancellationToken);
        if (accessibleSections != null)
            filter.SectionIds = accessibleSections;

        var result = await _academicService.GetAssessmentsAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves an assessment by ID.
    /// </summary>
    [HttpGet("assessments/{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssessmentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _academicService.GetAssessmentByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        if (result.Data != null && !await CanAccessSectionAsync(result.Data.SectionId, cancellationToken))
            return Forbid();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the grading roster for an assessment with enrolled students and existing marks.
    /// </summary>
    [HttpGet("assessments/{id}/roster")]
    [ProducesResponseType(typeof(ApiResponse<ExamRosterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ExamRosterDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAssessmentRoster(Guid id, CancellationToken cancellationToken)
    {
        if (CurrentRoles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase))
            return Forbid();
        var assessment = await _store.GetAssessmentByIdAsync(id, cancellationToken);
        if (assessment == null || !await CanAccessSectionAsync(assessment.SectionId, cancellationToken))
            return Forbid();

        var result = await _marksService.GetExamRosterAsync(id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Records marks in bulk for an entire section's assessment roster.
    /// </summary>
    [HttpPost("marks/batch")]
    [RequirePermission(SystemPermission.AcademicsRecord)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordMarksBatch([FromBody] RecordMarksBatchDto request, CancellationToken cancellationToken)
    {
        var assessment = await _store.GetAssessmentByIdAsync(request.AssessmentId, cancellationToken);
        if (assessment == null || !await CanAccessSectionAsync(assessment.SectionId, cancellationToken))
            return Forbid();

        var result = await _marksService.RecordMarksBatchAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Updates a single student mark record.
    /// </summary>
    [HttpPut("marks/{markId}")]
    [RequirePermission(SystemPermission.AcademicsRecord)]
    [ProducesResponseType(typeof(ApiResponse<StudentMarkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentMarkDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStudentMark(Guid markId, [FromBody] UpdateStudentMarkDto request, CancellationToken cancellationToken)
    {
        var mark = await _store.GetMarkByIdAsync(markId, cancellationToken);
        if (mark != null && !await CanAccessStudentAsync(mark.StudentId, cancellationToken))
            return Forbid();

        var result = await _marksService.UpdateStudentMarkAsync(markId, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all evaluated marks for a given assessment.
    /// </summary>
    [HttpGet("marks/assessment/{assessmentId}")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentMarkDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarksByAssessment(Guid assessmentId, CancellationToken cancellationToken)
    {
        if (CurrentRoles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase))
            return Forbid();
        var assessment = await _store.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (assessment == null || !await CanAccessSectionAsync(assessment.SectionId, cancellationToken))
            return Forbid();

        var result = await _marksService.GetMarksByAssessmentAsync(assessmentId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full history of exam marks for a specific student.
    /// </summary>
    [HttpGet("marks/student/{studentId}")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentMarkDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarksByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _marksService.GetMarksByStudentAsync(studentId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Generates a comprehensive student report card including GPA and core subject failure detection.
    /// </summary>
    [HttpGet("analytics/report-card/{studentId}")]
    [RequirePermission(SystemPermission.AcademicsReports)]
    [ProducesResponseType(typeof(ApiResponse<StudentReportCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentReportCardDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStudentReportCard(Guid studentId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _analyticsService.GenerateReportCardAsync(studentId, academicYearId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Evaluates academic trajectory and detects Academic Shock (sudden score drop >= 15%).
    /// </summary>
    [HttpGet("analytics/trajectory/{studentId}")]
    [RequirePermission(SystemPermission.AcademicsReports)]
    [ProducesResponseType(typeof(ApiResponse<AcademicTrajectoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AcademicTrajectoryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAcademicTrajectory(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _analyticsService.GetAcademicTrajectoryAsync(studentId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves students failing an assessment.
    /// </summary>
    [HttpGet("analytics/failing/{assessmentId}")]
    [RequirePermission(SystemPermission.AcademicsReports)]
    [ProducesResponseType(typeof(ApiResponse<FailingStudentsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FailingStudentsSummaryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFailingStudents(Guid assessmentId, CancellationToken cancellationToken)
    {
        if (CurrentRoles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase))
            return Forbid();
        var assessment = await _store.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (assessment == null || !await CanAccessSectionAsync(assessment.SectionId, cancellationToken))
            return Forbid();

        var result = await _analyticsService.GetFailingStudentsForAssessmentAsync(assessmentId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Identifies all students within a school who are at academic risk due to failing core subjects.
    /// </summary>
    [HttpGet("analytics/at-risk/school/{schoolId}")]
    [RequirePermission(SystemPermission.AcademicsReports)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentReportCardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAtRiskAcademicStudents(Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetAtRiskAcademicStudentsBySchoolAsync(schoolId, cancellationToken);
        var visibleStudentIds = await _accessPolicy.GetVisibleStudentIdsAsync(CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);
        if (visibleStudentIds != null && result.Data != null)
            result.Data = result.Data.Where(student => visibleStudentIds.Contains(student.StudentId)).ToList();
        return Ok(result);
    }

    private Task<bool> CanAccessStudentAsync(Guid studentId, CancellationToken cancellationToken)
        => _accessPolicy.CanAccessStudentAsync(studentId, CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);

    private Task<bool> CanAccessSectionAsync(Guid sectionId, CancellationToken cancellationToken)
        => _accessPolicy.CanAccessSectionAsync(sectionId, CurrentUserId, CurrentRoles, cancellationToken);

    private async Task<HashSet<Guid>?> AccessibleSectionIdsAsync(CancellationToken cancellationToken)
    {
        if (CurrentRoles.Any(StudentAccessPolicy.IsAdministrator) ||
            CurrentRoles.Contains(UserRoleType.Counselor, StringComparer.OrdinalIgnoreCase))
            return null;

        if (CurrentRoles.Contains(UserRoleType.Teacher, StringComparer.OrdinalIgnoreCase) && CurrentUserId.HasValue)
            return (await _store.GetSectionsByTeacherUserIdAsync(CurrentUserId.Value, cancellationToken))
                .Select(section => section.Id)
                .ToHashSet();

        if (CurrentRoles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase) && CurrentStudentId.HasValue)
        {
            var student = await _store.GetStudentByIdAsync(CurrentStudentId.Value, cancellationToken);
            return student?.CurrentSectionId is Guid sectionId ? new HashSet<Guid> { sectionId } : new HashSet<Guid>();
        }

        return new HashSet<Guid>();
    }
}
