using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Roles;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IEduGuardDataStore _dataStore;

    public RolePermissionService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dataStore.GetAllRolesAsync(cancellationToken);
        var dtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var permissions = await _dataStore.GetPermissionsForRoleAsync(role.Id, cancellationToken);
            dtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                Permissions = permissions
            });
        }

        return ApiResponse<List<RoleDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<RoleDto>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _dataStore.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return ApiResponse<RoleDto>.Fail($"Role '{roleName}' was not found.");
        }

        var permissions = await _dataStore.GetPermissionsForRoleAsync(role.Id, cancellationToken);
        var dto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            Permissions = permissions
        };

        return ApiResponse<RoleDto>.Ok(dto);
    }

    public async Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<RoleDto>.Fail("Role name cannot be empty.");
        }

        var existing = await _dataStore.GetRoleByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            return ApiResponse<RoleDto>.Fail($"A role named '{request.Name}' already exists.");
        }

        var newRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            IsSystemRole = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddRoleAsync(newRole, cancellationToken);

        if (request.Permissions != null && request.Permissions.Count > 0)
        {
            await _dataStore.SetRolePermissionsAsync(newRole.Id, request.Permissions, cancellationToken);
        }

        var permissions = await _dataStore.GetPermissionsForRoleAsync(newRole.Id, cancellationToken);

        return ApiResponse<RoleDto>.Ok(new RoleDto
        {
            Id = newRole.Id,
            Name = newRole.Name,
            Description = newRole.Description,
            IsSystemRole = newRole.IsSystemRole,
            Permissions = permissions
        }, "Role created successfully.");
    }

    public async Task<ApiResponse> AssignRoleToUserAsync(AssignRoleDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dataStore.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return ApiResponse.FailResult("User not found.");
        }

        var role = await _dataStore.GetRoleByNameAsync(request.RoleName, cancellationToken);
        if (role == null)
        {
            return ApiResponse.FailResult($"Role '{request.RoleName}' not found.");
        }

        var existingRoles = await _dataStore.GetRolesForUserAsync(user.Id, cancellationToken);
        if (existingRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            return ApiResponse.OkResult($"User is already assigned to role '{role.Name}'.");
        }

        await _dataStore.AddUserRoleAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse.OkResult($"Role '{role.Name}' assigned to user successfully.");
    }

    public async Task<ApiResponse> RemoveRoleFromUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _dataStore.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return ApiResponse.FailResult($"Role '{roleName}' not found.");
        }

        await _dataStore.RemoveUserRoleAsync(userId, role.Id, cancellationToken);
        return ApiResponse.OkResult($"Role '{roleName}' removed from user.");
    }

    public Task<ApiResponse<List<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<PermissionDto>
        {
            new() { Code = SystemPermission.UsersManage, Module = "Authentication", Description = "Manage user accounts and credentials" },
            new() { Code = SystemPermission.RolesManage, Module = "Authentication", Description = "Configure roles and permission policies" },
            new() { Code = SystemPermission.SchoolsRead, Module = "Schools", Description = "View school profiles and campus information" },
            new() { Code = SystemPermission.SchoolsWrite, Module = "Schools", Description = "Create and modify school details" },
            new() { Code = SystemPermission.StudentsRead, Module = "Students", Description = "View student records and demographics" },
            new() { Code = SystemPermission.StudentsWrite, Module = "Students", Description = "Enroll, modify, or update student profiles" },
            new() { Code = SystemPermission.StudentsHistoryRead, Module = "Students", Description = "View longitudinal student historical timeline" },
            new() { Code = SystemPermission.AttendanceRead, Module = "Attendance", Description = "View attendance registers and calendar summaries" },
            new() { Code = SystemPermission.AttendanceRecord, Module = "Attendance", Description = "Mark and edit daily attendance" },
            new() { Code = SystemPermission.AttendanceAnalytics, Module = "Attendance", Description = "Analyze chronic absenteeism and patterns" },
            new() { Code = SystemPermission.AcademicsRead, Module = "Academics", Description = "View subject scores and terminal report cards" },
            new() { Code = SystemPermission.AcademicsRecord, Module = "Academics", Description = "Enter and modify student assessment marks" },
            new() { Code = SystemPermission.AcademicsReports, Module = "Academics", Description = "Generate institutional academic performance reports" },
            new() { Code = SystemPermission.DropoutAlertsRead, Module = "DropoutAnalysis", Description = "View at-risk students and early warning triggers" },
            new() { Code = SystemPermission.DropoutRiskCalculate, Module = "DropoutAnalysis", Description = "Trigger recalculation of Dropout Risk Index (DRI)" },
            new() { Code = SystemPermission.InterventionsManage, Module = "Interventions", Description = "Log and manage student retention interventions" }
        };

        return Task.FromResult(ApiResponse<List<PermissionDto>>.Ok(list));
    }

    public async Task<ApiResponse<List<string>>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _dataStore.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return ApiResponse<List<string>>.Fail($"Role '{roleName}' not found.");
        }

        var perms = await _dataStore.GetPermissionsForRoleAsync(role.Id, cancellationToken);
        return ApiResponse<List<string>>.Ok(perms);
    }

    public async Task<ApiResponse> UpdateRolePermissionsAsync(string roleName, UpdateRolePermissionsDto request, CancellationToken cancellationToken = default)
    {
        var role = await _dataStore.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return ApiResponse.FailResult($"Role '{roleName}' not found.");
        }

        await _dataStore.SetRolePermissionsAsync(role.Id, request.Permissions, cancellationToken);
        return ApiResponse.OkResult($"Permissions updated for role '{roleName}'.");
    }
}
