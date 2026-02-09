using System.Diagnostics;
using MathInvaders.Models;
using Microsoft.AspNetCore.Mvc;

namespace MathInvaders.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //public IActionResult Index()
        //{
        //    return View(new HomeViewModel());
        //}
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                PlayerId = HttpContext.Session.GetString("PlayerId") ?? ""
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
