using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class AnalyticsController : Controller
    {
        public IActionResult Analytics()
        {
            return View();
        }
    }
}
