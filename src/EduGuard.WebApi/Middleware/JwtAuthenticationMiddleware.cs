using System.Security.Claims;
using EduGuard.Application.Interfaces;

namespace EduGuard.WebApi.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IEduGuardDataStore dataStore)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var principal = tokenService.GetPrincipalFromExpiredToken(token);

            if (principal != null)
            {
                // Verify exp claim hasn't expired
                var expClaim = principal.FindFirst("exp");
                if (expClaim != null && long.TryParse(expClaim.Value, out var expSeconds))
                {
                    var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                    if (expDate > DateTimeOffset.UtcNow)
                    {
                        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (Guid.TryParse(userIdValue, out var userId))
                        {
                            var user = await dataStore.GetUserByIdAsync(userId, context.RequestAborted);
                            if (user != null && user.IsActive && !user.IsLockedOut &&
                                (!user.LockoutEndUtc.HasValue || user.LockoutEndUtc.Value <= DateTime.UtcNow))
                            {
                                var claims = principal.Claims
                                    .Where(claim => claim.Type != ClaimTypes.Role &&
                                                    claim.Type != "permission" &&
                                                    claim.Type != "student_id" &&
                                                    claim.Type != "school_id")
                                    .ToList();
                                if (user.SchoolId.HasValue)
                                    claims.Add(new Claim("school_id", user.SchoolId.Value.ToString()));
                                if (user.LinkedStudentId.HasValue)
                                    claims.Add(new Claim("student_id", user.LinkedStudentId.Value.ToString()));
                                foreach (var role in await dataStore.GetRolesForUserAsync(userId, context.RequestAborted))
                                    claims.Add(new Claim(ClaimTypes.Role, role));
                                foreach (var permission in await dataStore.GetPermissionsForUserAsync(userId, context.RequestAborted))
                                    claims.Add(new Claim("permission", permission));
                                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "EduGuardAuth"));
                            }
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
