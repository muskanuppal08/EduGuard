using System.Security.Claims;
using EduGuard.Application.DTOs.Auth;
using EduGuard.Application.DTOs.Profiles;
using EduGuard.Application.DTOs.Roles;
using EduGuard.Domain.Enums;
using EduGuard.Infrastructure.Persistence;
using EduGuard.Infrastructure.Security;
using EduGuard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduGuard.UnitTests;

public class AuthTests
{
    private readonly InMemoryEduGuardDataStore _dataStore;
    private readonly PasswordHasher _passwordHasher;
    private readonly TokenService _tokenService;
    private readonly AuthService _authService;
    private readonly RolePermissionService _roleService;
    private readonly UserProfileService _profileService;

    public AuthTests()
    {
        _dataStore = new InMemoryEduGuardDataStore();
        _passwordHasher = new PasswordHasher();

        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "Super_Secure_Secret_Key_For_EduGuard_Testing_2026!",
            ["Jwt:Issuer"] = "EduGuardTest",
            ["Jwt:Audience"] = "EduGuardAudience",
            ["Jwt:AccessTokenLifetimeMinutes"] = "60",
            ["Jwt:RefreshTokenLifetimeDays"] = "7"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        _tokenService = new TokenService(config);

        var seeder = new DataSeeder(_dataStore, _passwordHasher, NullLogger<DataSeeder>.Instance);
        seeder.SeedAsync().GetAwaiter().GetResult();

        _authService = new AuthService(_dataStore, _passwordHasher, _tokenService, NullLogger<AuthService>.Instance);
        _roleService = new RolePermissionService(_dataStore);
        _profileService = new UserProfileService(_dataStore);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerifySuccessfully()
    {
        // Arrange
        string password = "SecurePassword@2026";

        // Act
        var (hash, salt) = _passwordHasher.HashPassword(password);
        bool isValid = _passwordHasher.VerifyPassword(password, hash, salt);
        bool isInvalid = _passwordHasher.VerifyPassword("WrongPassword", hash, salt);

        // Assert
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task AuthService_Login_WithValidCredentials_ShouldSucceed()
    {
        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        Assert.Equal("admin", result.Data.User.Username);
        Assert.Contains(UserRoleType.SuperAdmin, result.Data.User.Roles);
        Assert.Contains(SystemPermission.UsersManage, result.Data.User.Permissions);
    }

    [Fact]
    public async Task AuthService_Login_WithInvalidPassword_ShouldFail()
    {
        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "admin",
            Password = "WrongPassword!"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Contains("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task AuthService_RegisterStaff_ShouldCreateStaffWithRole()
    {
        // Arrange
        var request = new RegisterStaffDto
        {
            Username = "teacher_smith",
            Email = "smith@school.edu",
            FullName = "John Smith",
            Password = "TeacherPassword123!",
            Role = UserRoleType.Teacher,
            Designation = "Senior Mathematics Teacher",
            Department = "Mathematics"
        };

        // Act
        var registerResult = await _authService.RegisterStaffAsync(request);

        // Assert
        Assert.True(registerResult.Success);
        Assert.NotNull(registerResult.Data);
        Assert.Contains(UserRoleType.Teacher, registerResult.Data.Roles);
        Assert.Contains(SystemPermission.AttendanceRecord, registerResult.Data.Permissions);

        // Verify login works with new credentials
        var loginResult = await _authService.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "teacher_smith",
            Password = "TeacherPassword123!"
        });
        Assert.True(loginResult.Success);
    }

    [Fact]
    public async Task RoleService_AssignAndRemoveRole_ShouldUpdatePermissions()
    {
        // Arrange: Create a user
        var registerResult = await _authService.RegisterStaffAsync(new RegisterStaffDto
        {
            Username = "counselor_jane",
            Email = "jane@school.edu",
            FullName = "Jane Doe",
            Password = "CounselorPassword123!",
            Role = UserRoleType.Counselor
        });
        Guid userId = registerResult.Data!.Id;

        // Act: Assign Teacher role as secondary role
        await _roleService.AssignRoleToUserAsync(new AssignRoleDto
        {
            UserId = userId,
            RoleName = UserRoleType.Teacher
        });

        var userProfile = await _profileService.GetUserProfileByIdAsync(userId);

        // Assert
        Assert.Contains(UserRoleType.Counselor, userProfile.Data!.Roles);
        Assert.Contains(UserRoleType.Teacher, userProfile.Data.Roles);

        // Act: Remove Teacher role
        await _roleService.RemoveRoleFromUserAsync(userId, UserRoleType.Teacher);
        var updatedProfile = await _profileService.GetUserProfileByIdAsync(userId);

        // Assert
        Assert.DoesNotContain(UserRoleType.Teacher, updatedProfile.Data!.Roles);
        Assert.Contains(UserRoleType.Counselor, updatedProfile.Data.Roles);
    }

    [Fact]
    public async Task UserProfileService_UpdateProfile_ShouldUpdateFields()
    {
        // Arrange
        var user = await _dataStore.GetUserByUsernameOrEmailAsync("admin");
        Assert.NotNull(user);

        // Act
        var updateResult = await _profileService.UpdateProfileAsync(user.Id, new UpdateProfileDto
        {
            FullName = "Chief Administrator Updated",
            PhoneNumber = "+9876543210",
            Designation = "Executive Director of Education",
            PreferredLanguage = "es"
        });

        // Assert
        Assert.True(updateResult.Success);
        Assert.Equal("Chief Administrator Updated", updateResult.Data!.FullName);
        Assert.Equal("+9876543210", updateResult.Data.PhoneNumber);
        Assert.Equal("Executive Director of Education", updateResult.Data.Designation);
        Assert.Equal("es", updateResult.Data.PreferredLanguage);
    }

    [Fact]
    public async Task AuthService_FailedAttempts_ShouldTriggerLockout()
    {
        // Act: Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            await _authService.LoginAsync(new LoginRequestDto
            {
                UsernameOrEmail = "admin",
                Password = "BadPassword"
            });
        }

        // 6th attempt should return lockout message
        var lockoutResult = await _authService.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!" // Even with right password
        });

        // Assert
        Assert.False(lockoutResult.Success);
        Assert.Contains("locked", lockoutResult.Message.ToLowerInvariant());
    }
}
