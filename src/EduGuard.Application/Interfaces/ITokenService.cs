using System.Security.Claims;
using EduGuard.Domain.Entities;

namespace EduGuard.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
