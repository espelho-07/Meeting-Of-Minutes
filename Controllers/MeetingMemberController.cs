using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingMemberController : Controller
    {
        public IActionResult MeetingMemberAddEdit()
        {
            return View();
        }

        public IActionResult MeetingMemberList()
        {
            return View();
        }
    }
}
