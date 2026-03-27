using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class InviteHistoryController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? searchtext, string? statusFilter, int page = 1)
        {
            if (!RoleAccessService.HasPermission(HttpContext, "users.manage.global"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            List<InviteHistoryEntryModel> allItems = UserInviteService.GetInviteHistory(searchtext, statusFilter);
            const int pageSize = 12;
            page = Math.Max(page, 1);

            ViewBag.SearchText = searchtext ?? string.Empty;
            ViewBag.StatusFilter = statusFilter ?? string.Empty;

            return View(new PagedListViewModel<InviteHistoryEntryModel>
            {
                Items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = allItems.Count,
                SearchText = searchtext
            });
        }

        [HttpGet]
        public IActionResult Preview(int id)
        {
            if (!RoleAccessService.HasPermission(HttpContext, "users.manage.global"))
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            string previewPath = UserInviteService.GetPreviewPathByInviteId(id);
            if (string.IsNullOrWhiteSpace(previewPath) || !System.IO.File.Exists(previewPath))
            {
                TempData["ErrorMessage"] = "Invite preview was not found.";
                return RedirectToAction(nameof(Index));
            }

            byte[] content = System.IO.File.ReadAllBytes(previewPath);
            return File(content, "text/html; charset=utf-8");
        }
    }
}
