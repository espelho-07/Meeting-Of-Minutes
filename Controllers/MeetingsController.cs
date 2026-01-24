using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsController : Controller
    {
        public IActionResult MeetingsAddEdit()
        {
            return View();
        }

        public IActionResult MeetingsList()
        {
            return View();
        }

        public IActionResult Save(DepartmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DepartmentAddEdit", model);
            }

            return RedirectToAction("DepartmentList");
        }
    }
}
