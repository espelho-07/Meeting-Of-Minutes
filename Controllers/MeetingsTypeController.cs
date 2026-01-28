using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsTypeController : Controller
    {
        public IActionResult MeetingsTypeAddEdit()
        {
            return View();
        }
        public IActionResult MeetingsTypeList()
        {
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
