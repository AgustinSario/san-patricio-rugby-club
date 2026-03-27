using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin,Control de Acceso")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Control de Acceso") && !User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Acceso");
            }
            // Real Data from DB
            var totalSociosActivos = await _context.Socios.CountAsync(s => s.EsActivo);
            
            // Morosos: Socios con al menos una cuota Vencida o Pendiente fuera de término
            var sociosMorosos = await _context.Socios
                .Where(s => s.EsActivo && s.Cuotas.Any(c => c.Estado == EstadoPago.Vencido))
                .CountAsync();

            ViewBag.SociosActivos = totalSociosActivos;
            ViewBag.SociosMorosos = sociosMorosos;
            ViewBag.SociosAlDia = totalSociosActivos - sociosMorosos;

            // Ultimos movimientos
            var ultimosPagos = await _context.Cuotas
                .Include(c => c.Socio)
                .Where(c => c.Estado == EstadoPago.Pagado)
                .OrderByDescending(c => c.FechaPago)
                .Take(5)
                .ToListAsync();

            return View(ultimosPagos);
        }
    }
}
