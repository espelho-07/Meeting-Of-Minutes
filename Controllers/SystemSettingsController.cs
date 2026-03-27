using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class SystemSettingsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (!RoleAccessService.HasPermission(HttpContext, "settings.manage"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            return View(SystemSettingsService.GetSettings());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(SystemSettingsModel model)
        {
            if (!RoleAccessService.HasPermission(HttpContext, "settings.manage"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            model.ModifiedBy = HttpContext.Session.GetString("UserName") ?? "System";
            SystemSettingsService.SaveSettings(model);

            AuditLogService.Log(
                HttpContext.Session.GetString("CompanyName"),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "SettingsUpdate",
                "SystemSettings",
                null,
                "System settings updated",
                "Platform configuration was updated by super admin.");

            TempData["SuccessMessage"] = "System settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
