using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Profiles;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

public class UsersController : BaseApiController
{
    private readonly IUserProfileService _userProfileService;
    private readonly IEduGuardDataStore _store;

    public UsersController(IUserProfileService userProfileService, IEduGuardDataStore store)
    {
        _userProfileService = userProfileService;
        _store = store;
    }

    /// <summary>
    /// Retrieves the profile of the currently logged-in user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("User not authenticated."));
        }

        var result = await _userProfileService.GetUserProfileByIdAsync(CurrentUserId.Value, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Updates the profile of the currently logged-in user.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto request, CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("User not authenticated."));
        }

        var result = await _userProfileService.UpdateProfileAsync(CurrentUserId.Value, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a user profile by ID. Requires UsersManage permission.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [RequirePermission(SystemPermission.UsersManage)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.GetUserProfileByIdAsync(userId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a paginated list of users with optional filtering. Requires UsersManage permission.
    /// </summary>
    [HttpGet]
    [RequirePermission(SystemPermission.UsersManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.GetUsersAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activates or deactivates a user account. Requires UsersManage permission.
    /// </summary>
    [HttpPut("{userId:guid}/status")]
    [RequirePermission(SystemPermission.UsersManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.ToggleUserStatusAsync(userId, isActive, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{userId:guid}/student-link")]
    [RequirePermission(SystemPermission.UsersManage)]
    public async Task<IActionResult> LinkStudentAccount(Guid userId, [FromBody] LinkStudentAccountDto request, CancellationToken cancellationToken)
    {
        var user = await _store.GetUserByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound(ApiResponse.FailResult("User account not found."));
        var roles = await _store.GetRolesForUserAsync(userId, cancellationToken);
        if (!roles.Contains(UserRoleType.StudentParent, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.FailResult("Only student or parent accounts can be linked to a student record."));

        var student = await _store.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null) return NotFound(ApiResponse.FailResult("Student record not found."));

        user.LinkedStudentId = student.Id;
        user.SchoolId = student.SchoolId;
        await _store.UpdateUserAsync(user, cancellationToken);
        var profile = await _userProfileService.GetUserProfileByIdAsync(userId, cancellationToken);
        return Ok(profile);
    }
}
