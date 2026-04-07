using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using SanPatricioRugby.Web.Services;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ImportService _importService;
        private readonly ApplicationDbContext _context;

        public AdminController(ImportService importService, ApplicationDbContext context)
        {
            _importService = importService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Importar
        public IActionResult Importar()
        {
            return View();
        }

        // POST: /Admin/Importar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Importar(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Por favor seleccione un archivo Excel válido.");
                return View();
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var result = await _importService.ImportFromExcelAsync(stream);
                
                TempData["SuccessMessage"] = $"Importación completada con éxito.\n" +
                                            $"Socios: {result.SociosNuevos} nuevos, {result.SociosActualizados} actualizados.\n" +
                                            $"Cuotas: {result.CuotasCreadas} creadas, {result.CuotasActualizadas} actualizadas.";
                
                return RedirectToAction(nameof(Importar));
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null) message += " " + ex.InnerException.Message;
                ModelState.AddModelError("", message);
                return View();
            }
        }

        // GET: /Admin/ConfigEmail
        public async Task<IActionResult> ConfigEmail()
        {
            var config = await _context.ConfiguracionesEmail.FirstOrDefaultAsync()
                         ?? new ConfiguracionEmail();
            return View(config);
        }

        // POST: /Admin/ConfigEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigEmail(ConfiguracionEmail model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.ConfiguracionesEmail.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.ConfiguracionesEmail.Add(model);
            }
            else
            {
                existing.EmailRemitente = model.EmailRemitente;
                existing.NombreRemitente = model.NombreRemitente;
                existing.SmtpHost = model.SmtpHost;
                existing.SmtpPort = model.SmtpPort;
                existing.SmtpUser = model.SmtpUser;
                existing.SmtpPassword = model.SmtpPassword;
                existing.UsarSsl = model.UsarSsl;
                
                // Nuevos campos del club
                existing.NombreClub = model.NombreClub;
                existing.RazonSocial = model.RazonSocial;
                existing.Domicilio = model.Domicilio;
                existing.Cuit = model.Cuit;
                existing.IngresosBrutos = model.IngresosBrutos;
                existing.InicioActividades = model.InicioActividades;
                existing.CondicionIva = model.CondicionIva;

                _context.ConfiguracionesEmail.Update(existing);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Configuración de email guardada correctamente.";
            return RedirectToAction(nameof(ConfigEmail));
        }
    }
}
