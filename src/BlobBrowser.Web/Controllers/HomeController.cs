using Microsoft.AspNetCore.Mvc;

namespace BlobBrowser.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error(string? message)
        {
            ViewData["ErrorMessage"] = message ?? "An error occurred.";
            return View();
        }
    }
}
