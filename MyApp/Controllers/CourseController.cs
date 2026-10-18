using Microsoft.AspNetCore.Mvc;

namespace MyApp.Controllers
{
    [Route("courses")]
    public class CourseController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return Content("All courses");
        }

        [HttpGet("{id:int}")]
        public IActionResult Details(int id)
        {
            return Content($"Course ID: {id}");
        }

        [HttpGet("category/{name}")]
        public IActionResult Category(string name)
        {
            return Content($"Category: {name}");
        }

        public IActionResult Return()
        {
            return RedirectToAction("Privacy", "Home");
        }
    }
}