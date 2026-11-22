using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.History;
using EduGuard.Application.DTOs.Students;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using EduGuard.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.StudentsRead)]
public class StudentsController : BaseApiController
{
    private readonly IStudentService _studentService;
    private readonly IStudentHistoryService _historyService;
    private readonly IStudentRiskService _riskService;
    private readonly IEduGuardDataStore _dataStore;
    private readonly StudentAccessPolicy _accessPolicy;

    public StudentsController(
        IStudentService studentService,
        IStudentHistoryService historyService,
        IStudentRiskService riskService,
        IEduGuardDataStore dataStore,
        StudentAccessPolicy accessPolicy)
    {
        _studentService = studentService;
        _historyService = historyService;
        _riskService = riskService;
        _dataStore = dataStore;
        _accessPolicy = accessPolicy;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchStudents([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(Array.Empty<object>());

        var risks = await _riskService.GetRiskSummaryAsync(cancellationToken);
        var riskById = risks.Students.ToDictionary(item => item.StudentId);
        var visibleStudentIds = await VisibleStudentIdsAsync(cancellationToken);
        var term = query.Trim();
        var matches = new List<object>();
        var page = 1;
        var totalPages = 1;
        while (page <= totalPages && matches.Count < 10)
        {
            var result = await _studentService.GetStudentsAsync(new StudentFilterDto { Page = page, PageSize = 100 }, cancellationToken);
            if (!result.Success || result.Data == null) break;
            totalPages = result.Data.TotalPages;
            foreach (var student in result.Data.Items)
            {
                if (visibleStudentIds != null && !visibleStudentIds.Contains(student.Id))
                    continue;

                if (!student.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !student.AdmissionNumber.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !(student.CurrentClassName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(riskById.TryGetValue(student.Id, out var matchedRisk) &&
                      (matchedRisk.RiskLevel.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                       matchedRisk.Factors.Any(factor => factor.Contains(term, StringComparison.OrdinalIgnoreCase)))))
                    continue;

                riskById.TryGetValue(student.Id, out var risk);
                matches.Add(new
                {
                    student.Id,
                    student.FullName,
                    student.AdmissionNumber,
                    ClassName = student.CurrentClassName,
                    RiskLevel = risk?.RiskLevel ?? "Unknown",
                    RiskScore = risk?.RiskScore ?? 0
                });
                if (matches.Count == 10) break;
            }
            page++;
        }

        return Ok(matches);
    }

    /// <summary>
    /// Registers a new student with demographic and socioeconomic vulnerability markers.
    /// </summary>
    [HttpPost]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto request, CancellationToken cancellationToken)
    {
        var result = await _studentService.RegisterStudentAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a 360-degree student profile including socioeconomic indicators and guardian details.
    /// </summary>
    [HttpGet("{studentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _studentService.GetStudentByIdAsync(studentId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a paginated list of students with filters for school, section, status, BPL, first-gen learner.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents([FromQuery] StudentFilterDto filter, CancellationToken cancellationToken)
    {
        var visibleStudentIds = await VisibleStudentIdsAsync(cancellationToken);
        if (visibleStudentIds == null)
            return Ok(await _studentService.GetStudentsAsync(filter, cancellationToken));

        var allVisible = new List<StudentDto>();
        var page = 1;
        ApiResponse<PagedResult<StudentDto>>? result;
        do
        {
            result = await _studentService.GetStudentsAsync(new StudentFilterDto
            {
                SchoolId = filter.SchoolId,
                SectionId = filter.SectionId,
                Status = filter.Status,
                Gender = filter.Gender,
                IsBPL = filter.IsBPL,
                IsFirstGenerationLearner = filter.IsFirstGenerationLearner,
                SearchTerm = filter.SearchTerm,
                IncludeArchived = filter.IncludeArchived,
                Page = page,
                PageSize = 100
            }, cancellationToken);
            if (!result.Success || result.Data == null) return Ok(result);
            allVisible.AddRange(result.Data.Items.Where(student => visibleStudentIds.Contains(student.Id)));
            page++;
        } while (page <= result.Data.TotalPages);

        var requestedPage = Math.Max(1, filter.Page);
        var requestedPageSize = Math.Clamp(filter.PageSize, 1, 100);
        var paged = new PagedResult<StudentDto>(
            allVisible.Skip((requestedPage - 1) * requestedPageSize).Take(requestedPageSize).ToList(),
            allVisible.Count,
            requestedPage,
            requestedPageSize);
        return Ok(ApiResponse<PagedResult<StudentDto>>.Ok(paged));
    }

    /// <summary>
    /// Updates student demographic and socioeconomic vulnerability data.
    /// </summary>
    [HttpPut("{studentId:guid}")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStudent(Guid studentId, [FromBody] UpdateStudentDto request, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _studentService.UpdateStudentAsync(studentId, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{studentId:guid}")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    public async Task<IActionResult> ArchiveStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (!CurrentRoles.Any(role => role is "Admin" or "SuperAdmin" or "DistrictAdmin" or "SchoolPrincipal"))
            return Forbid();

        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
            return NotFound(ApiResponse<object>.Fail("Student not found."));
        if (student.CurrentStatus == StudentStatus.Archived)
            return Ok(ApiResponse<object>.Ok(new { student.Id, student.CurrentStatus }));

        var previousStatus = student.CurrentStatus;
        student.ArchivedFromStatus = previousStatus;
        student.CurrentStatus = StudentStatus.Archived;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);
        var historyResult = await _historyService.AddTimelineNoteAsync(new AddTimelineNoteDto
        {
            StudentId = studentId,
            Title = "Student record archived",
            Description = $"Student record was archived by an administrator. Previous status: {previousStatus}."
        }, CurrentUserId, cancellationToken);
        if (!historyResult.Success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("The record was archived, but its history entry could not be saved."));

        return Ok(ApiResponse<object>.Ok(new { student.Id, student.CurrentStatus }));
    }

    [HttpPut("{studentId:guid}/restore")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    public async Task<IActionResult> RestoreStudent(Guid studentId, CancellationToken cancellationToken)
    {
        if (!CurrentRoles.Any(role => role is "Admin" or "SuperAdmin" or "DistrictAdmin" or "SchoolPrincipal"))
            return Forbid();

        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
            return NotFound(ApiResponse<object>.Fail("Student not found."));
        if (student.CurrentStatus != StudentStatus.Archived)
            return BadRequest(ApiResponse<object>.Fail("Only archived student records can be restored."));

        student.CurrentStatus = student.ArchivedFromStatus ?? StudentStatus.Active;
        student.ArchivedFromStatus = null;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);
        var historyResult = await _historyService.AddTimelineNoteAsync(new AddTimelineNoteDto
        {
            StudentId = studentId,
            Title = "Student record restored",
            Description = "Student record was restored to Active status by an administrator."
        }, CurrentUserId, cancellationToken);
        if (!historyResult.Success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("The record was restored, but its history entry could not be saved."));

        return Ok(ApiResponse<object>.Ok(new { student.Id, student.CurrentStatus }));
    }

    /// <summary>
    /// Retrieves the chronological longitudinal event timeline for a student.
    /// </summary>
    [HttpGet("{studentId:guid}/history")]
    [RequirePermission(SystemPermission.StudentsHistoryRead)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentTimelineItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentTimeline(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        var result = await _historyService.GetStudentHistoryTimelineAsync(studentId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Appends a staff/counselor observation note to the student's timeline.
    /// </summary>
    [HttpPost("{studentId:guid}/notes")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse<StudentTimelineItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddTimelineNote(Guid studentId, [FromBody] AddTimelineNoteDto request, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId, cancellationToken))
            return Forbid();

        request.StudentId = studentId;
        var result = await _historyService.AddTimelineNoteAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    private Task<HashSet<Guid>?> VisibleStudentIdsAsync(CancellationToken cancellationToken)
        => _accessPolicy.GetVisibleStudentIdsAsync(CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);

    private Task<bool> CanAccessStudentAsync(Guid studentId, CancellationToken cancellationToken)
        => _accessPolicy.CanAccessStudentAsync(studentId, CurrentUserId, CurrentStudentId, CurrentRoles, cancellationToken);
}
