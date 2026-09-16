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

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
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
                        context.User = principal;
                    }
                }
            }
        }

        await _next(context);
    }
}
