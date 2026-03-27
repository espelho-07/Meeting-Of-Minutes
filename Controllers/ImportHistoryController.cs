using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class ImportHistoryController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? moduleName)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            List<ImportHistoryEntryModel> entries = ImportHistoryService.GetAllByCompany(GetCurrentCompanyName());
            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                entries = entries
                    .Where(x => string.Equals(x.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.ModuleName = moduleName ?? string.Empty;
            ViewBag.AvailableModules = entries.Select(x => x.ModuleName).Distinct().OrderBy(x => x).ToList();
            return View(entries);
        }

        private bool IsAdminUser()
        {
            return RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
        }

        private string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
        }
    }
}
