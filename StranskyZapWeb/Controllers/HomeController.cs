using Microsoft.AspNetCore.Mvc;

namespace StranskyZapWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Login));
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Cadastro()
        {
            return View();
        }
        public IActionResult Conversa()
        {
            return View();
        }
    }
}
