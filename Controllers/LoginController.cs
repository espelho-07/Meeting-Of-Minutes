using Microsoft.AspNetCore.Mvc;

namespace Meeting_Of_Minutes.Controllers
{
    public class LoginController : Controller
    {
        #region Actions
        public IActionResult Login()
        {
            return View();
        }
        #endregion
    }
}




