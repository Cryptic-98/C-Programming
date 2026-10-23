using Microsoft.AspNetCore.Mvc;
using MyPracticeApp.Models;

namespace MyPracticeApp.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }
    }
}
