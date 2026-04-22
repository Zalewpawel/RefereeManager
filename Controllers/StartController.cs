using Microsoft.AspNetCore.Mvc;

namespace Sedziowanie.Controllers
{
    public class StartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
