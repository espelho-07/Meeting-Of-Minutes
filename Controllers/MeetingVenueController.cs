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
    }
}
