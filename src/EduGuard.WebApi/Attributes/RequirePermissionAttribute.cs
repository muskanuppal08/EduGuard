using System.Security.Claims;
using System.Text.Json;
using EduGuard.Application.DTOs.Common;
using EduGuard.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduGuard.WebApi.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            var unauthorized = ApiResponse.FailResult("Authentication is required to access this resource.");
            context.Result = new JsonResult(unauthorized) { StatusCode = StatusCodes.Status401Unauthorized };
            return Task.CompletedTask;
        }

        // SuperAdmin always has full access
        if (user.IsInRole(UserRoleType.SuperAdmin))
        {
            return Task.CompletedTask;
        }

        bool hasPermission = user.Claims.Any(c => c.Type == "permission" &&
            c.Value.Equals(_permission, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            var forbidden = ApiResponse.FailResult($"Forbidden: Missing required permission '{_permission}'.");
            context.Result = new JsonResult(forbidden) { StatusCode = StatusCodes.Status403Forbidden };
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
