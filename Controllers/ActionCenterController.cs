using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class ActionCenterController : Controller
    {
        public IActionResult Index(string? scope, string? searchtext)
        {
            bool isAdminUser = IsAdminUser();
            ActionCenterViewModel viewModel = ActionCenterService.BuildViewModel(
                isAdminUser,
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetInt32("StaffID"));
            viewModel.CurrentScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
            viewModel.SearchText = searchtext?.Trim() ?? string.Empty;
            viewModel.FilteredPriorityQueue = ActionCenterService.ApplyQueueFilters(viewModel.PriorityQueue, viewModel.CurrentScope, viewModel.SearchText);

            return View(viewModel);
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
