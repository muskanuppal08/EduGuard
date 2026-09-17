using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Roles;

namespace EduGuard.Application.Interfaces;

public interface IRolePermissionService
{
    Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<RoleDto>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse> AssignRoleToUserAsync(AssignRoleDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse> RemoveRoleFromUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<List<string>>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<ApiResponse> UpdateRolePermissionsAsync(string roleName, UpdateRolePermissionsDto request, CancellationToken cancellationToken = default);
}
