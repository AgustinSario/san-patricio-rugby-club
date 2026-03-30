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

        public AdminController(ImportService importService)
        {
            _importService = importService;
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
    }
}
