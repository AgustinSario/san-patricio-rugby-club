using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using SanPatricioRugby.Web.Services;
using System;
using System.Threading.Tasks;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin,Control de Acceso")]
    public class AccesoController : Controller
    {
        private readonly IAccesoService _accesoService;
        private readonly ApplicationDbContext _context;

        public AccesoController(IAccesoService accesoService, ApplicationDbContext context)
        {
            _accesoService = accesoService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string term)
        {
            if (string.IsNullOrEmpty(term)) return BadRequest();

            // Try as ID first, then as DNI
            if (int.TryParse(term, out int id))
            {
                try {
                    var status = await _accesoService.GetSocioStatusAsync(id);
                    ViewBag.PrecioMoroso = await _accesoService.GetPrecioConceptoAsync("Entrada Socio Moroso");
                    return View("Resultado", status);
                } catch { }
            }

            var statusByDni = await _accesoService.GetSocioStatusByDniAsync(term);
            if (statusByDni != null)
            {
                ViewBag.PrecioMoroso = await _accesoService.GetPrecioConceptoAsync("Entrada Socio Moroso");
                return View("Resultado", statusByDni);
            }

            // If not found, it's a non-member
            ViewData["Term"] = term;
            ViewBag.PrecioNoSocio = await _accesoService.GetPrecioConceptoAsync("Entrada No Socio");
            return View("NoEncontrado");
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarIngreso(int? socioId, TipoIngreso tipo, decimal monto)
        {
            var registro = new RegistroIngreso
            {
                SocioId = socioId == 0 ? null : socioId,
                Tipo = tipo,
                MontoPagado = monto,
                Fecha = DateTime.Now
            };

            await _accesoService.RegistrarIngresoAsync(registro);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Estacionamiento()
        {
            var precios = await _accesoService.GetPreciosAsync();
            return View(precios);
        }

        [HttpPost]
        public async Task<IActionResult> CobrarEstacionamiento(TipoVehiculo tipo, decimal monto)
        {
            var registro = new RegistroEstacionamiento
            {
                Vehiculo = tipo,
                MontoPagado = monto,
                Fecha = DateTime.Now
            };

            await _accesoService.RegistrarEstacionamientoAsync(registro);
            return Json(new { success = true });
        }

        public async Task<IActionResult> Reporte(DateTime? fecha)
        {
            var date = fecha ?? DateTime.Today;
            var reporte = await _accesoService.GetRecaudacionDiaAsync(date);
            return View(reporte);
        }

        public async Task<IActionResult> Precios()
        {
            var precios = await _accesoService.GetPreciosAsync();
            return View(precios);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPrecio(int id, decimal valor)
        {
            var precio = await _context.Precios.FindAsync(id);
            if (precio != null)
            {
                precio.Valor = valor;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
