using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Meeting_Of_Minutes.Filters
{
    public class CheckAccess : ActionFilterAttribute
    {
        private static readonly HashSet<string> AdminOnlyControllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Department",
            "Staff",
            "MeetingsType",
            "MeetingVenue",
            "MeetingMember"
        };

        private static readonly HashSet<string> MeetingsAdminOnlyActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MeetingsAddEdit",
            "Save",
            "Delete",
            "ExportToExcel",
            "AddMeetingMember",
            "UpdateMeetingMemberAttendance",
            "DeleteMeetingMember"
        };

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

                if (!userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    bool isAdminOnlyRoute = AdminOnlyControllers.Contains(controllerName)
                        || (controllerName.Equals("Meetings", StringComparison.OrdinalIgnoreCase) && MeetingsAdminOnlyActions.Contains(actionName));

                    if (isAdminOnlyRoute)
                    {
                        context.Result = new RedirectToActionResult("DashBoard", "DashBoard", null);
                        base.OnActionExecuting(context);
                        return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
