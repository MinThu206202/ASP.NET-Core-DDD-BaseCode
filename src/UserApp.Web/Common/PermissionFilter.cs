using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Web.Common;

public class PermissionFilter : IAsyncActionFilter
{
    private readonly IPermissionChecker _permissionService;

    public PermissionFilter(IPermissionChecker permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // =====================================================
        // 1. SKIP IF [AllowAnonymous]
        // =====================================================
        var endpoint = context.ActionDescriptor.EndpointMetadata;

        if (endpoint.Any(x => x is AllowAnonymousAttribute))
        {
            await next();
            return;
        }

        // =====================================================
        // 2. AUTH CHECK
        // =====================================================
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            SetApiJsonResult(context, 401, "Unauthorized");
            return;
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
        {
            SetApiJsonResult(context, 401, "Unauthorized");
            return;
        }

        var userId = Guid.Parse(userIdClaim);

        // =====================================================
        // 3. BUILD PERMISSION FROM ROUTE
        // =====================================================
        var controller = context.ActionDescriptor.RouteValues["controller"] ?? "";
        var action = context.ActionDescriptor.RouteValues["action"] ?? "";

        if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
        {
            await next();
            return;
        }

        // Strip "Api" suffix so "RoleApi" -> "Roles" for permission matching
        if (controller.EndsWith("Api", StringComparison.OrdinalIgnoreCase))
        {
            controller = controller[..^3];
        }

        // Map API action names to MVC permission names
        action = action switch
        {
            "GetAll" => "Index",
            "Get" => "Details",
            "Update" => "Edit",
            "GetRoles" => "ManageRoles",
            "UpdateRoles" => "ManageRoles",
            "UploadMedia" => "Upload",
            "DeleteMedia" => "Delete",
            "GetMedia" => "Index",
            _ => action
        };

        // Media actions use the "Media" permission prefix instead of entity name
        if ((action is "Upload" or "Delete") && controller != "Media")
        {
            controller = "Media";
        }

        var permission = $"{controller}.{action}";

        // =====================================================
        // 4. CHECK DB PERMISSION
        // =====================================================
        var hasPermission = await _permissionService.HasPermissionAsync(
            userId,
            permission
        );

        if (!hasPermission)
        {
            SetApiJsonResult(context, 403, "Forbidden");
            return;
        }

        // =====================================================
        // 5. ALLOW REQUEST
        // =====================================================
        await next();
    }

    private static void SetApiJsonResult(ActionExecutingContext context, int statusCode, string message)
    {
        var isApi = context.HttpContext.Request.Path.StartsWithSegments("/api");
        if (isApi)
        {
            var json = JsonSerializer.Serialize(new { success = false, message });
            context.Result = new ContentResult
            {
                StatusCode = statusCode,
                Content = json,
                ContentType = "application/json"
            };
        }
        else
        {
            context.Result = statusCode == 401 ? new UnauthorizedResult() : new ForbidResult();
        }
    }
}
