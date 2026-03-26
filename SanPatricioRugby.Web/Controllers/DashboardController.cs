using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Dummy Data for Phase 1 Display
            ViewBag.SociosActivos = 854;
            ViewBag.SociosMorosos = 112;
            ViewBag.SociosAlDia = 742;

            return View();
        }
    }
}
