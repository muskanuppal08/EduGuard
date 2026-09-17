using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Roles;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using EduGuard.WebApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[RequirePermission(SystemPermission.RolesManage)]
public class RolesController : BaseApiController
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolesController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    /// <summary>
    /// Retrieves all roles in the system with their assigned permissions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.GetAllRolesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific role by name with its permissions.
    /// </summary>
    [HttpGet("{roleName}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleByName(string roleName, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.GetRoleByNameAsync(roleName, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Creates a custom role with optional initial permissions.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto request, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.CreateRoleAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPost("assign")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto request, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.AssignRoleToUserAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Removes an assigned role from a user.
    /// </summary>
    [HttpDelete("users/{userId:guid}/roles/{roleName}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveRoleFromUser(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.RemoveRoleFromUserAsync(userId, roleName, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all system permissions categorized by module.
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.GetAllPermissionsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the permissions mapped to a role.
    /// </summary>
    [HttpPut("{roleName}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRolePermissions(string roleName, [FromBody] UpdateRolePermissionsDto request, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.UpdateRolePermissionsAsync(roleName, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
