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

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
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
}
