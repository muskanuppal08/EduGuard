using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Profiles;

namespace EduGuard.Application.Interfaces;

public interface IUserProfileService
{
    Task<ApiResponse<UserProfileDto>> GetUserProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<UserProfileDto>>> GetUsersAsync(UserFilterDto filter, CancellationToken cancellationToken = default);
    Task<ApiResponse> ToggleUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}
