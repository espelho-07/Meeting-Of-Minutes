using System.Diagnostics;
using demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace demo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SelectAllDepartment()
        {
            return View("SelectAllDepartment");
        }
    }
}
