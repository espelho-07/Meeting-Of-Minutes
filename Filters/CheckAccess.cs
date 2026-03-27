using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Meeting_Of_Minutes.Services;
using Meeting_Of_Minutes.Models;

namespace Meeting_Of_Minutes.Filters
{
    public class CheckAccess : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool isAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(m => m.GetType().Name == "AllowAnonymousAttribute");

            if (!isAllowAnonymous)
            {
                int? userId = context.HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                    base.OnActionExecuting(context);
                    return;
                }

                string controllerName = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
                string actionName = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
                string userRole = context.HttpContext.Session.GetString("UserRole") ?? string.Empty;
                bool forcePasswordReset = string.Equals(context.HttpContext.Session.GetString("ForcePasswordReset"), "true", StringComparison.OrdinalIgnoreCase);

                if (forcePasswordReset &&
                    !controllerName.Equals("Profile", StringComparison.OrdinalIgnoreCase) &&
                    !controllerName.Equals("Auth", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToActionResult("Profile", "Profile", null);
                    base.OnActionExecuting(context);
                    return;
                }

                if (!RoleAccessService.CanAccessRoute(context.HttpContext, controllerName, actionName))
                {
                    context.Result = new RedirectToActionResult("DashBoard", "DashBoard", null);
                    base.OnActionExecuting(context);
                    return;
                }

                if (context.Controller is Controller controller)
                {
                    AppShellViewModel shellModel = ShellService.Build(context.HttpContext, context.RouteData);
                    controller.ViewData["AppShellModel"] = shellModel;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
