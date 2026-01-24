using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class DepartmentController : Controller
    {
        public IActionResult DepartmentAddEdit()
        {
            return View();
        }

        public IActionResult DepartmentList()
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
