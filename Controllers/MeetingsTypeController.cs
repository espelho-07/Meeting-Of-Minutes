using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsTypeController : Controller
    {
        // LIST
        public IActionResult MeetingsTypeList()
        {
            return View();
        }

        public IActionResult MeetingsTypeAddEdit(int? id)
        {

            return View();
        }

        public IActionResult MeetingsTypeDetails(int id)
        {

            ViewBag.MeetingID = id;
            return View();
        }

        
        public IActionResult Save(MeetingTypeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingsTypeAddEdit", model);
            }

            return RedirectToAction("MeetingsTypeList");
        }
    }
}
