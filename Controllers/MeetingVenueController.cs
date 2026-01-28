using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingVenueController : Controller
    {
        public IActionResult MeetingVenueAddEdit()
        {
            return View();
        }

        public IActionResult MeetingVenueList()
        {
            return View();
        }

        public IActionResult Save(MeetingVenueModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingVenueAddEdit", model);
            }

            return RedirectToAction("MeetingVenueList");
        }
    }
}
