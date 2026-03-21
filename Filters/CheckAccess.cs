using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
                string? userName = context.HttpContext.Session.GetString("UserName");
                if (string.IsNullOrEmpty(userName))
                {
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
