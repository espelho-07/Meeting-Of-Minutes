using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class RoleManagementController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (!RoleAccessService.HasPermission(HttpContext, "roles.manage"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            return View(RoleAccessService.BuildRoleManagementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(string[]? permissionSelection)
        {
            if (!RoleAccessService.HasPermission(HttpContext, "roles.manage"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            Dictionary<string, List<string>> selections = new(StringComparer.OrdinalIgnoreCase);
            foreach (string role in RoleAccessService.GetEditableRoles())
            {
                selections[role] = new List<string>();
            }

            foreach (string value in permissionSelection ?? Array.Empty<string>())
            {
                string[] parts = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                string role = parts[0];
                string permissionKey = parts[1];
                if (selections.ContainsKey(role))
                {
                    selections[role].Add(permissionKey);
                }
            }

            RoleAccessService.SaveRolePermissions(selections);
            AuditLogService.Log(
                HttpContext.Session.GetString("CompanyName"),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "RolePermissionsUpdate",
                "RoleManagement",
                null,
                "Role permissions updated",
                "Admin and user permission matrix was updated by Super Admin.");

            TempData["SuccessMessage"] = "Role permissions updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
