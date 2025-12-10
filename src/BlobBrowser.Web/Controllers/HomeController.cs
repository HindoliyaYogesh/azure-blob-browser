using Microsoft.AspNetCore.Mvc;
using BlobBrowser.Services;

namespace BlobBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBlobBrowserService _svc;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IBlobBrowserService svc, ILogger<HomeController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? path)
        {
            try
            {
                var (items, breadcrumbs) = await _svc.ListAsync(path);
                ViewData["Path"] = path ?? "";
                ViewData["Breadcrumbs"] = breadcrumbs;
                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing blobs");
                return View("Error", ex.Message);
            }
        }
    }
}
