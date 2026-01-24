using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult StaffAddEdit()
        {
            return View();
        }
        public IActionResult StaffList()
        {
            return View();
        }

    }
}
