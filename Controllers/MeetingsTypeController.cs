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
    }
}
