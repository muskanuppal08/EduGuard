using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EduGuard.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                      User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    protected string CurrentUsername =>
        User.FindFirst(ClaimTypes.Name)?.Value ??
        User.FindFirst("unique_name")?.Value ??
        string.Empty;

    protected string CurrentUserEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value ??
        string.Empty;

    protected Guid? CurrentSchoolId
    {
        get
        {
            var schoolIdClaim = User.FindFirst("school_id")?.Value;
            return Guid.TryParse(schoolIdClaim, out var id) ? id : null;
        }
    }

    protected List<string> CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    protected List<string> CurrentPermissions =>
        User.FindAll("permission").Select(c => c.Value).ToList();
}
