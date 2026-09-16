using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Schools;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.SchoolsRead)]
public class SchoolsController : BaseApiController
{
    private readonly ISchoolService _schoolService;

    public SchoolsController(ISchoolService schoolService)
    {
        _schoolService = schoolService;
    }

    /// <summary>
    /// Registers a new school with area classification and marginalized indicators.
    /// </summary>
    [HttpPost]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolDto request, CancellationToken cancellationToken)
    {
        var result = await _schoolService.CreateSchoolAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details and institutional statistics for a school.
    /// </summary>
    [HttpGet("{schoolId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchoolById(Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _schoolService.GetSchoolByIdAsync(schoolId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a paginated list of schools with optional filters for district and marginalized area status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SchoolDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchools([FromQuery] SchoolFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _schoolService.GetSchoolsAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates administrative details of a school.
    /// </summary>
    [HttpPut("{schoolId:guid}")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchoolDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSchool(Guid schoolId, [FromBody] UpdateSchoolDto request, CancellationToken cancellationToken)
    {
        var result = await _schoolService.UpdateSchoolAsync(schoolId, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
