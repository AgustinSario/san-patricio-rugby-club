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
            var currentYear = 2025; // Year for current sync
            var currentMonth = DateTime.Now.Month;
            
            // Morosos: Socios con al menos una cuota no pagada en lo que va del 2025
            var sociosMorosos = await _context.Socios
                .Where(s => s.EsActivo && s.Cuotas.Any(c => c.Anio == currentYear && c.Mes <= currentMonth && c.Estado != EstadoPago.Pagado))
                .CountAsync();

            ViewBag.SociosActivos = totalSociosActivos;
            ViewBag.SociosMorosos = sociosMorosos;
            ViewBag.SociosAlDia = totalSociosActivos - sociosMorosos;

            // Socios Becados: Activos con "BECA" en TipoSocio o Acuerdos
            var becados = await _context.Socios
                .Where(s => s.EsActivo && 
                            ((s.TipoSocio != null && s.TipoSocio.ToUpper().Contains("BECA")) || 
                             (s.Acuerdos != null && s.Acuerdos.ToUpper().Contains("BECA"))))
                .OrderBy(s => s.ApellidoNombre)
                .ToListAsync();

            return View(becados);
        }
    }
}
