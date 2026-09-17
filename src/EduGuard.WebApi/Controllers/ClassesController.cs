using EduGuard.Application.DTOs.Classes;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.SchoolsRead)]
public class ClassesController : BaseApiController
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    /// <summary>
    /// Creates a new academic year calendar period.
    /// </summary>
    [HttpPost("academic-years")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse<AcademicYearDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearDto request, CancellationToken cancellationToken)
    {
        var result = await _classService.CreateAcademicYearAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists all configured academic years.
    /// </summary>
    [HttpGet("academic-years")]
    [ProducesResponseType(typeof(ApiResponse<List<AcademicYearDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAcademicYears(CancellationToken cancellationToken)
    {
        var result = await _classService.GetAcademicYearsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the currently active academic year.
    /// </summary>
    [HttpGet("academic-years/current")]
    [ProducesResponseType(typeof(ApiResponse<AcademicYearDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AcademicYearDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentAcademicYear(CancellationToken cancellationToken)
    {
        var result = await _classService.GetCurrentAcademicYearAsync(cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Creates a grade/standard in a school.
    /// </summary>
    [HttpPost("grades")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse<ClassGradeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGrade([FromBody] CreateGradeDto request, CancellationToken cancellationToken)
    {
        var result = await _classService.CreateGradeAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Lists all grades and sections configured for a school.
    /// </summary>
    [HttpGet("schools/{schoolId:guid}/grades")]
    [ProducesResponseType(typeof(ApiResponse<List<ClassGradeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGradesBySchool(Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _classService.GetGradesBySchoolAsync(schoolId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a section within a grade for an academic year.
    /// </summary>
    [HttpPost("sections")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse<SectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSection([FromBody] CreateSectionDto request, CancellationToken cancellationToken)
    {
        var result = await _classService.CreateSectionAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all sections within a grade.
    /// </summary>
    [HttpGet("grades/{gradeId:guid}/sections")]
    [ProducesResponseType(typeof(ApiResponse<List<SectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionsByGrade(Guid gradeId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
    {
        var result = await _classService.GetSectionsByGradeAsync(gradeId, academicYearId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single section by ID.
    /// </summary>
    [HttpGet("sections/{sectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionById(Guid sectionId, CancellationToken cancellationToken)
    {
        var result = await _classService.GetSectionByIdAsync(sectionId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Assigns or updates the designated class teacher for a section.
    /// </summary>
    [HttpPost("sections/assign-teacher")]
    [RequirePermission(SystemPermission.SchoolsWrite)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignClassTeacher([FromBody] AssignClassTeacherDto request, CancellationToken cancellationToken)
    {
        var result = await _classService.AssignClassTeacherAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
