using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class DepartmentController : Controller
    {
        public IActionResult DepartmentAddEdit(int? id)
        {
            return View();
        }

        public IActionResult DepartmentList()
        {
            return View();
        }

        public IActionResult DepartmentView(int id)
        {

            ViewBag.DepartmentID = id;
            ViewBag.DepartmentName = "Customer Service";
            ViewBag.Created = DateTime.Parse("2025-11-20 10:45:09");
            ViewBag.Modified = DateTime.Parse("2025-11-20 22:38:36");

            return View();
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
