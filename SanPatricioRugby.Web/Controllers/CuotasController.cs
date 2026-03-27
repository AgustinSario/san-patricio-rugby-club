using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using SanPatricioRugby.Web.Models;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CuotasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuotasController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Pagar(int id, MedioPago medio)
        {
            var cuota = await _context.Cuotas.FindAsync(id);
            if (cuota == null) return NotFound();

            cuota.Estado = EstadoPago.Pagado;
            cuota.FechaPago = DateTime.Now;
            cuota.MedioPagoUtilizado = medio;

            _context.Update(cuota);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: Cuotas/Delete/5
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
