using EduGuard.Application.DTOs.Auth;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and generates JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Revokes an active refresh token (single device logout).
    /// </summary>
    [HttpPost("revoke-token")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeTokenAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Revokes all active sessions for the currently authenticated user.
    /// </summary>
    [HttpPost("revoke-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(ApiResponse.FailResult("User not authenticated."));
        }

        var result = await _authService.RevokeAllUserTokensAsync(CurrentUserId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Registers a new staff member (Teacher, Principal, Counselor). Requires UsersManage permission.
    /// </summary>
    [HttpPost("register-staff")]
    [RequirePermission(SystemPermission.UsersManage)]
    [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterStaffAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Changes password for the currently authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(ApiResponse.FailResult("User not authenticated."));
        }

        var result = await _authService.ChangePasswordAsync(CurrentUserId.Value, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
