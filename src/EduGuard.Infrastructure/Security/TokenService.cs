using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace EduGuard.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly int _refreshTokenLifetimeDays;

    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? "EduGuard_Super_Secret_Key_For_Jwt_Generation_2026_Minimum_32_Chars!";
        _issuer = configuration["Jwt:Issuer"] ?? "EduGuard";
        _audience = configuration["Jwt:Audience"] ?? "EduGuardUsers";
        _accessTokenLifetimeMinutes = int.TryParse(configuration["Jwt:AccessTokenLifetimeMinutes"], out var mins) ? mins : 60;
        _refreshTokenLifetimeDays = int.TryParse(configuration["Jwt:RefreshTokenLifetimeDays"], out var days) ? days : 7;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes);
        long expSeconds = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
        long iatSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var header = new Dictionary<string, object>
        {
            { "alg", "HS256" },
            { "typ", "JWT" }
        };

        var roleList = roles.ToList();
        var permList = permissions.ToList();

        var payload = new Dictionary<string, object>
        {
            { "sub", user.Id.ToString() },
            { "nameid", user.Id.ToString() },
            { "unique_name", user.Username },
            { "email", user.Email },
            { "full_name", user.FullName },
            { "iss", _issuer },
            { "aud", _audience },
            { "iat", iatSeconds },
            { "exp", expSeconds },
            { "roles", roleList },
            { "permissions", permList }
        };

        if (user.SchoolId.HasValue)
        {
            payload["school_id"] = user.SchoolId.Value.ToString();
        }

        string headerJson = JsonSerializer.Serialize(header);
        string payloadJson = JsonSerializer.Serialize(payload);

        string headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        string payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        string dataToSign = $"{headerEncoded}.{payloadEncoded}";
        byte[] keyBytes = Encoding.UTF8.GetBytes(_secretKey);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        string signatureEncoded = Base64UrlEncode(signatureBytes);

        string token = $"{dataToSign}.{signatureEncoded}";
        return (token, expiresAtUtc);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress)
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        string rawToken = Convert.ToBase64String(randomBytes);
        string hash = HashToken(rawToken);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            string headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));

            // Verify signature
            byte[] keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            byte[] expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"));
            byte[] actualSig = Base64UrlDecode(parts[2]);

            if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var claims = new List<Claim>();

            if (root.TryGetProperty("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? ""));

            if (root.TryGetProperty("unique_name", out var uname))
                claims.Add(new Claim(ClaimTypes.Name, uname.GetString() ?? ""));

            if (root.TryGetProperty("email", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.GetString() ?? ""));

            if (root.TryGetProperty("full_name", out var fname))
                claims.Add(new Claim("full_name", fname.GetString() ?? ""));

            if (root.TryGetProperty("school_id", out var schoolId))
                claims.Add(new Claim("school_id", schoolId.GetString() ?? ""));

            if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in roles.EnumerateArray())
                {
                    claims.Add(new Claim(ClaimTypes.Role, r.GetString() ?? ""));
                }
            }

            if (root.TryGetProperty("permissions", out var perms) && perms.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in perms.EnumerateArray())
                {
                    claims.Add(new Claim("permission", p.GetString() ?? ""));
                }
            }

            var identity = new ClaimsIdentity(claims, "EduGuardAuth");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    public static string HashToken(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string incoming = input.Replace('-', '+').Replace('_', '/');
        switch (incoming.Length % 4)
        {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        return Convert.FromBase64String(incoming);
    }
}
