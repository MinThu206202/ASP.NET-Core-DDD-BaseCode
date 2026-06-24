using Microsoft.AspNetCore.Mvc.Filters;

namespace UserApp.Web.Common;

public class AuditContextActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

        context.HttpContext.Items["AuditPageName"] = controller;
        context.HttpContext.Items["AuditFunctionName"] = action;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
