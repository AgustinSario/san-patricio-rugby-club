using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using SanPatricioRugby.Web.Models;
using SanPatricioRugby.Web.Services;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CuotasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IReciboService _reciboService;

        public CuotasController(ApplicationDbContext context, IEmailService emailService, IReciboService reciboService)
        {
            _context = context;
            _emailService = emailService;
            _reciboService = reciboService;
        }

        // GET: Cuotas
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var cuotas = from c in _context.Cuotas.Include(c => c.Socio)
                         select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                cuotas = cuotas.Where(c => c.Socio.ApellidoNombre.Contains(searchString));
            }

            cuotas = cuotas.OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes);

            int pageSize = 10;
            return View(await PaginatedList<Cuota>.CreateAsync(cuotas.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Cuotas/Create
        public IActionResult Create(int? socioId)
        {
            if (socioId.HasValue)
            {
                ViewData["SocioId"] = new SelectList(_context.Socios, "Id", "ApellidoNombre", socioId.Value);
            }
            else
            {
                ViewData["SocioId"] = new SelectList(_context.Socios, "Id", "ApellidoNombre");
            }
            
            var model = new Cuota { 
                Anio = DateTime.Now.Year, 
                Mes = DateTime.Now.Month,
                SocioId = socioId ?? 0,
                FechaVencimiento = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10)
            };
            
            return View(model);
        }

        // POST: Cuotas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cuota cuota)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cuota);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SocioId"] = new SelectList(_context.Socios, "Id", "ApellidoNombre", cuota.SocioId);
            return View(cuota);
        }

        // GET: Cuotas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cuota = await _context.Cuotas.FindAsync(id);
            if (cuota == null) return NotFound();
            
            ViewData["SocioId"] = new SelectList(_context.Socios, "Id", "ApellidoNombre", cuota.SocioId);
            return View(cuota);
        }

        // POST: Cuotas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cuota cuota)
        {
            if (id != cuota.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cuota);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CuotaExists(cuota.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SocioId"] = new SelectList(_context.Socios, "Id", "ApellidoNombre", cuota.SocioId);
            return View(cuota);
        }

        // POST: Cuotas/Pagar/5
        [HttpPost]
        public async Task<IActionResult> Pagar(int id, MedioPago medio, string? emailSocio = null)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Socio)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (cuota == null) return NotFound();

            cuota.Estado = EstadoPago.Pagado;
            cuota.FechaPago = DateTime.Now;
            cuota.MedioPagoUtilizado = medio;

            // Si se proporcionó email y el socio no lo tenía, guardarlo
            if (!string.IsNullOrWhiteSpace(emailSocio) && cuota.Socio != null && string.IsNullOrWhiteSpace(cuota.Socio.Email))
            {
                cuota.Socio.Email = emailSocio.Trim();
                _context.Update(cuota.Socio);
            }

            _context.Update(cuota);
            await _context.SaveChangesAsync();

            // Determinar el email de destino
            var emailDestino = !string.IsNullOrWhiteSpace(emailSocio) ? emailSocio.Trim() : cuota.Socio?.Email;

            // Si no hay email aún, indicarlo al front para que lo pida
            if (string.IsNullOrWhiteSpace(emailDestino))
            {
                return Json(new { ok = true, emailEnviado = false, necesitaEmail = true, cuotaId = cuota.Id });
            }

            // Verificar si hay config de email antes de intentar enviar
            if (!await _emailService.HayConfiguracionAsync())
            {
                return Json(new { ok = true, emailEnviado = false, necesitaEmail = false, warning = "No hay configuración SMTP. Pagó registrado pero no se envió el recibo." });
            }

            // Generar PDF y enviar email
            try
            {
                var config = await _context.ConfiguracionesEmail.FirstOrDefaultAsync();
                var pdf = _reciboService.GenerarReciboPdf(cuota, cuota.Socio!, config);
                var enviado = await _emailService.EnviarReciboAsync(cuota, cuota.Socio!, pdf, emailDestino);
                return Json(new { ok = true, emailEnviado = enviado, necesitaEmail = false });
            }
            catch (Exception ex)
            {
                return Json(new { ok = true, emailEnviado = false, necesitaEmail = false, warning = "Pago registrado pero hubo un error al enviar el recibo: " + ex.Message });
            }
        }

        // POST: Cuotas/DescargarRecibo/5  
        [HttpGet]
        public async Task<IActionResult> DescargarRecibo(int id)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Socio)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cuota == null) return NotFound();

            var config = await _context.ConfiguracionesEmail.FirstOrDefaultAsync();
            var pdf = _reciboService.GenerarReciboPdf(cuota, cuota.Socio!, config);
            return File(pdf, "application/pdf", $"Recibo_{cuota.Socio!.ApellidoNombre.Replace(" ", "_")}_{cuota.Mes:00}_{cuota.Anio}.pdf");
        }

        // POST: Cuotas/EnviarReciboEmail - Envía el recibo una vez capturado el email del socio
        [HttpPost]
        public async Task<IActionResult> EnviarReciboEmail(int cuotaId, string email)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Socio)
                .FirstOrDefaultAsync(c => c.Id == cuotaId);

            if (cuota == null)
                return Json(new { enviado = false, error = "Cuota no encontrada" });

            // Guardar email en el socio si no lo tenía
            if (cuota.Socio != null && string.IsNullOrWhiteSpace(cuota.Socio.Email))
            {
                cuota.Socio.Email = email.Trim();
                _context.Update(cuota.Socio);
                await _context.SaveChangesAsync();
            }

            if (!await _emailService.HayConfiguracionAsync())
                return Json(new { enviado = false, error = "No hay configuración SMTP guardada." });

            try
            {
                var config = await _context.ConfiguracionesEmail.FirstOrDefaultAsync();
                var pdf = _reciboService.GenerarReciboPdf(cuota, cuota.Socio!, config);
                var enviado = await _emailService.EnviarReciboAsync(cuota, cuota.Socio!, pdf, email.Trim());
                return Json(new { enviado });
            }
            catch (Exception ex)
            {
                return Json(new { enviado = false, error = ex.Message });
            }
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cuota = await _context.Cuotas
                .Include(c => c.Socio)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cuota == null) return NotFound();

            return View(cuota);
        }

        // POST: Cuotas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cuota = await _context.Cuotas.FindAsync(id);
            if (cuota != null)
            {
                _context.Cuotas.Remove(cuota);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CuotaExists(int id)
        {
            return _context.Cuotas.Any(e => e.Id == id);
        }
    }
}
