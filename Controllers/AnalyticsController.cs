using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class AnalyticsController : Controller
    {
        #region Actions
        public IActionResult Analytics()
        {
            return View();
        }
        #endregion
    }
}




