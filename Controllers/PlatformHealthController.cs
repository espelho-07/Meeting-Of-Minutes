using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class PlatformHealthController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (!RoleAccessService.HasPermission(HttpContext, "settings.manage"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            return View(PlatformHealthService.Build());
        }
    }
}
