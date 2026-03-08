using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class ProfileController : Controller
    {
        #region Actions
        public IActionResult Profile()
        {
            return View();
        }
        #endregion
    }
}




