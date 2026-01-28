using Meeting_Of_Minutes.Models;
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
        public IActionResult Save(StaffModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("StaffAddEdit", model);
            }

            return RedirectToAction("StaffList");
        }

    }
}
