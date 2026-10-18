using Microsoft.AspNetCore.Mvc;

namespace MyApp.Controllers
{
    [Route("products")]
    public class ProductController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return Content("All products");
        }

        [HttpGet("{id:int}")]
        public IActionResult Details(int id)
        {
            return Content($"Product ID: {id}");
        }

        [HttpGet("category/{category}")]
        public IActionResult Category(string category)
        {
            return Content($"Category: {category}");
        }

        [HttpGet("year/{year:int:min(2000)}")]
        public IActionResult Year(int year)
        {
            return Content($"Year: {year}");
        }

        [HttpGet("search")]
        public IActionResult Search(string name)
        {
            return Content($"Name: {name}");
        }

        [HttpGet("page/{number:int?}")]
        public IActionResult Page(int number = 1)
        {
            return Content($"Page Number: {number}");
        }
    }
}