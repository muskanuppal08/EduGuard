using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Profiles;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IEduGuardDataStore _dataStore;

    public UserProfileService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<UserProfileDto>> GetUserProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dataStore.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.");
        }

        var profile = await _dataStore.GetProfileByUserIdAsync(userId, cancellationToken);
        var roles = await _dataStore.GetRolesForUserAsync(userId, cancellationToken);

        var dto = MapToDto(user, profile, roles);
        return ApiResponse<UserProfileDto>.Ok(dto);
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dataStore.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber.Trim();
        }

        await _dataStore.UpdateUserAsync(user, cancellationToken);

        var profile = await _dataStore.GetProfileByUserIdAsync(userId, cancellationToken) ?? new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        profile.Designation = request.Designation ?? profile.Designation;
        profile.Department = request.Department ?? profile.Department;
        profile.AvatarUrl = request.AvatarUrl ?? profile.AvatarUrl;
        profile.Address = request.Address ?? profile.Address;
        profile.PreferredLanguage = request.PreferredLanguage ?? profile.PreferredLanguage;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _dataStore.UpsertProfileAsync(profile, cancellationToken);

        var roles = await _dataStore.GetRolesForUserAsync(userId, cancellationToken);
        var dto = MapToDto(user, profile, roles);

        return ApiResponse<UserProfileDto>.Ok(dto, "Profile updated successfully.");
    }

    public async Task<ApiResponse<PagedResult<UserProfileDto>>> GetUsersAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        Func<User, bool> predicate = u =>
        {
            if (filter.SchoolId.HasValue && u.SchoolId != filter.SchoolId)
                return false;

            if (filter.IsActive.HasValue && u.IsActive != filter.IsActive)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                bool matches = u.Username.ToLowerInvariant().Contains(term) ||
                                u.Email.ToLowerInvariant().Contains(term) ||
                                u.FullName.ToLowerInvariant().Contains(term);
                if (!matches) return false;
            }

            return true;
        };

        var users = await _dataStore.QueryUsersAsync(predicate, page, pageSize, cancellationToken);
        var totalCount = await _dataStore.CountUsersAsync(predicate, cancellationToken);

        var dtos = new List<UserProfileDto>();
        foreach (var user in users)
        {
            var profile = await _dataStore.GetProfileByUserIdAsync(user.Id, cancellationToken);
            var roles = await _dataStore.GetRolesForUserAsync(user.Id, cancellationToken);

            if (!string.IsNullOrWhiteSpace(filter.Role) &&
                !roles.Contains(filter.Role, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            dtos.Add(MapToDto(user, profile, roles));
        }

        var result = new PagedResult<UserProfileDto>(dtos, totalCount, page, pageSize);
        return ApiResponse<PagedResult<UserProfileDto>>.Ok(result);
    }

    public async Task<ApiResponse> ToggleUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _dataStore.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse.FailResult("User not found.");
        }

        user.IsActive = isActive;
        if (!isActive)
        {
            // Revoke active tokens when deactivated
            await _dataStore.RevokeUserTokensAsync(userId, cancellationToken);
        }

        await _dataStore.UpdateUserAsync(user, cancellationToken);
        string statusText = isActive ? "activated" : "deactivated";
        return ApiResponse.OkResult($"User account has been {statusText}.");
    }

    private static UserProfileDto MapToDto(User user, UserProfile? profile, List<string> roles)
    {
        return new UserProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            SchoolId = user.SchoolId,
            Designation = profile?.Designation,
            Department = profile?.Department,
            AvatarUrl = profile?.AvatarUrl,
            Address = profile?.Address,
            PreferredLanguage = profile?.PreferredLanguage ?? "en",
            IsActive = user.IsActive,
            Roles = roles,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
