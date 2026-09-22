using EduGuard.Application.DTOs.Auth;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse> RevokeTokenAsync(RevokeTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserInfoDto>> RegisterStaffAsync(RegisterStaffDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> RegisterPublicAsync(RegisterPublicUserDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
}
