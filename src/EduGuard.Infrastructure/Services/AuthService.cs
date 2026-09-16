using EduGuard.Application.DTOs.Auth;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace EduGuard.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IEduGuardDataStore _dataStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(
        IEduGuardDataStore dataStore,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _dataStore = dataStore;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<LoginResponseDto>.Fail("Username/Email and password are required.");
        }

        var user = await _dataStore.GetUserByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken);
        if (user == null)
        {
            return ApiResponse<LoginResponseDto>.Fail("Invalid credentials.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail("Account is deactivated. Please contact your school administrator.");
        }

        if (user.IsLockedOut)
        {
            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                var remaining = user.LockoutEndUtc.Value - DateTime.UtcNow;
                return ApiResponse<LoginResponseDto>.Fail($"Account is temporarily locked. Try again in {Math.Ceiling(remaining.TotalMinutes)} minutes.");
            }
            // Lockout expired
            user.IsLockedOut = false;
            user.LockoutEndUtc = null;
            user.AccessFailedCount = 0;
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!isPasswordValid)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.IsLockedOut = true;
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("User {Username} locked out due to repeated failed login attempts.", user.Username);
            }
            await _dataStore.UpdateUserAsync(user, cancellationToken);
            return ApiResponse<LoginResponseDto>.Fail("Invalid credentials.");
        }

        // Successful authentication
        user.AccessFailedCount = 0;
        user.IsLockedOut = false;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _dataStore.UpdateUserAsync(user, cancellationToken);

        var roles = await _dataStore.GetRolesForUserAsync(user.Id, cancellationToken);
        var permissions = await _dataStore.GetPermissionsForUserAsync(user.Id, cancellationToken);

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, request.IpAddress);
        await _dataStore.AddRefreshTokenAsync(refreshToken, cancellationToken);

        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "." + refreshToken.Id.ToString("N"),
            ExpiresAtUtc = expiresAtUtc,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                SchoolId = user.SchoolId,
                Roles = roles,
                Permissions = permissions
            }
        };

        return ApiResponse<LoginResponseDto>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse<LoginResponseDto>.Fail("Refresh token is required.");
        }

        string hash = TokenService.HashToken(request.RefreshToken);
        var existingToken = await _dataStore.GetRefreshTokenAsync(hash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail("Invalid or expired refresh token.");
        }

        var user = await _dataStore.GetUserByIdAsync(existingToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail("User account is no longer active.");
        }

        // Rotate token
        existingToken.IsRevoked = true;
        existingToken.RevokedAtUtc = DateTime.UtcNow;

        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, request.IpAddress);
        existingToken.ReplacedByToken = newRefreshToken.TokenHash;

        await _dataStore.UpdateRefreshTokenAsync(existingToken, cancellationToken);
        await _dataStore.AddRefreshTokenAsync(newRefreshToken, cancellationToken);

        var roles = await _dataStore.GetRolesForUserAsync(user.Id, cancellationToken);
        var permissions = await _dataStore.GetPermissionsForUserAsync(user.Id, cancellationToken);

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(user, roles, permissions);

        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "." + newRefreshToken.Id.ToString("N"),
            ExpiresAtUtc = expiresAtUtc,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                SchoolId = user.SchoolId,
                Roles = roles,
                Permissions = permissions
            }
        };

        return ApiResponse<LoginResponseDto>.Ok(response, "Token refreshed successfully.");
    }

    public async Task<ApiResponse> RevokeTokenAsync(RevokeTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse.FailResult("Refresh token is required.");
        }

        string hash = TokenService.HashToken(request.RefreshToken);
        var token = await _dataStore.GetRefreshTokenAsync(hash, cancellationToken);
        if (token != null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = DateTime.UtcNow;
            await _dataStore.UpdateRefreshTokenAsync(token, cancellationToken);
        }

        return ApiResponse.OkResult("Token revoked successfully.");
    }

    public async Task<ApiResponse> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dataStore.RevokeUserTokensAsync(userId, cancellationToken);
        return ApiResponse.OkResult("All active sessions revoked.");
    }

    public async Task<ApiResponse<UserInfoDto>> RegisterStaffAsync(RegisterStaffDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Username))
        {
            return ApiResponse<UserInfoDto>.Fail("Username and email are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return ApiResponse<UserInfoDto>.Fail("Password must be at least 6 characters long.");
        }

        var existingUser = await _dataStore.GetUserByUsernameOrEmailAsync(request.Username, cancellationToken);
        if (existingUser != null)
        {
            return ApiResponse<UserInfoDto>.Fail("Username is already taken.");
        }

        var existingEmail = await _dataStore.GetUserByUsernameOrEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            return ApiResponse<UserInfoDto>.Fail("Email is already in use.");
        }

        var role = await _dataStore.GetRoleByNameAsync(request.Role, cancellationToken);
        if (role == null)
        {
            return ApiResponse<UserInfoDto>.Fail($"Specified role '{request.Role}' does not exist.");
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            PhoneNumber = request.PhoneNumber,
            SchoolId = request.SchoolId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddUserAsync(newUser, cancellationToken);

        await _dataStore.AddUserRoleAsync(new UserRole
        {
            UserId = newUser.Id,
            RoleId = role.Id,
            AssignedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Designation = request.Designation,
            Department = request.Department,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dataStore.UpsertProfileAsync(profile, cancellationToken);

        var permissions = await _dataStore.GetPermissionsForUserAsync(newUser.Id, cancellationToken);

        var userInfo = new UserInfoDto
        {
            Id = newUser.Id,
            Username = newUser.Username,
            Email = newUser.Email,
            FullName = newUser.FullName,
            SchoolId = newUser.SchoolId,
            Roles = new List<string> { role.Name },
            Permissions = permissions
        };

        return ApiResponse<UserInfoDto>.Ok(userInfo, "Staff member registered successfully.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dataStore.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse.FailResult("User not found.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            return ApiResponse.FailResult("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return ApiResponse.FailResult("New password must be at least 6 characters long.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return ApiResponse.FailResult("New password cannot be the same as the current password.");
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _dataStore.UpdateUserAsync(user, cancellationToken);

        // Invalidate other sessions on password change for security
        await _dataStore.RevokeUserTokensAsync(userId, cancellationToken);

        return ApiResponse.OkResult("Password changed successfully. Please log in again.");
    }
}
