using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Enrollments;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.StudentsRead)]
public class EnrollmentsController : BaseApiController
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    /// <summary>
    /// Enrolls a student into a section and academic year.
    /// </summary>
    [HttpPost]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentDto request, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.EnrollStudentAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Batch promotes students from one section to another for a new academic year.
    /// </summary>
    [HttpPost("batch-promote")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BatchPromote([FromBody] BatchPromoteDto request, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.BatchPromoteStudentsAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Records a student dropout event with date, exit reason, and audit remarks.
    /// </summary>
    [HttpPost("dropout")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordDropout([FromBody] RecordDropoutDto request, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.RecordDropoutAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Transfers a student to another school.
    /// </summary>
    [HttpPost("transfer")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransferStudent([FromBody] TransferStudentDto request, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.TransferStudentAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Re-enrolls a dropped out student returning to school.
    /// </summary>
    [HttpPost("re-enroll")]
    [RequirePermission(SystemPermission.StudentsWrite)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReEnroll([FromBody] ReEnrollDto request, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.ReEnrollStudentAsync(request, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all historical and current enrollments for a student.
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<EnrollmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentEnrollments(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetStudentEnrollmentsAsync(studentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all active students currently enrolled in a section.
    /// </summary>
    [HttpGet("section/{sectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<EnrollmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionEnrollments(Guid sectionId, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetSectionEnrollmentsAsync(sectionId, cancellationToken);
        return Ok(result);
    }
}
