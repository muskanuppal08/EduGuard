using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.History;
using EduGuard.Application.DTOs.Students;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.StudentsRead)]
public class StudentsController : BaseApiController
{
    private readonly IStudentService _studentService;
    private readonly IStudentHistoryService _historyService;

    public StudentsController(IStudentService studentService, IStudentHistoryService historyService)
    {
        _studentService = studentService;
        _historyService = historyService;
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
        var result = await _studentService.GetStudentsAsync(filter, cancellationToken);
        return Ok(result);
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
        var result = await _studentService.UpdateStudentAsync(studentId, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the chronological longitudinal event timeline for a student.
    /// </summary>
    [HttpGet("{studentId:guid}/history")]
    [RequirePermission(SystemPermission.StudentsHistoryRead)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentTimelineItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentTimeline(Guid studentId, CancellationToken cancellationToken)
    {
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
        request.StudentId = studentId;
        var result = await _historyService.AddTimelineNoteAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
