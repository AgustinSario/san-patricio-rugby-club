using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using SanPatricioRugby.Web.Models;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SociosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SociosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Socios
        public async Task<IActionResult> Index(string searchString, string searchDni, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDni"] = searchDni;

            var socios = from s in _context.Socios
                         select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                socios = socios.Where(s => s.ApellidoNombre.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(searchDni))
            {
                socios = socios.Where(s => s.Dni.Contains(searchDni));
            }

            socios = socios.OrderBy(s => s.ApellidoNombre);

            int pageSize = 10;
            return View(await PaginatedList<Socio>.CreateAsync(socios.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Socios/SocioMorosos
        public async Task<IActionResult> SocioMorosos(string searchString, string searchDni, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDni"] = searchDni;

            var currentYear = 2025; // Year for current sync
            var currentMonth = DateTime.Now.Month;
            
            // Query base de morosos
            var query = _context.Socios
                .Include(s => s.Cuotas)
                .Where(s => s.EsActivo && s.Cuotas.Any(c => c.Anio == currentYear && c.Mes <= currentMonth && c.Estado != EstadoPago.Pagado));

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.ApellidoNombre.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(searchDni))
            {
                query = query.Where(s => s.Dni.Contains(searchDni));
            }

            query = query.OrderBy(s => s.ApellidoNombre);

            int pageSize = 10;
            // Retornamos especificando la ruta completa de la vista para evitar problemas de resolución
            return View("~/Views/Socios/SocioMorosos.cshtml", await PaginatedList<Socio>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> ExportarMorosos()
        {
            var currentYear = 2025;
            var currentMonth = DateTime.Now.Month;
            var socios = await _context.Socios
                .Include(s => s.Cuotas)
                .Where(s => s.EsActivo && s.Cuotas.Any(c => c.Anio == currentYear && c.Mes <= currentMonth && c.Estado != EstadoPago.Pagado))
                .ToListAsync();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Morosos");
                worksheet.Cell(1, 1).Value = "Socio";
                worksheet.Cell(1, 2).Value = "DNI";
                worksheet.Cell(1, 3).Value = "Celular";
                worksheet.Cell(1, 4).Value = "Meses Deuda";
                worksheet.Cell(1, 5).Value = "Monto Total";

                int row = 2;
                foreach (var s in socios)
                {
                    var cuotas = s.Cuotas.Where(c => c.Anio == currentYear && c.Estado != EstadoPago.Pagado).ToList();
                    worksheet.Cell(row, 1).Value = s.ApellidoNombre;
                    worksheet.Cell(row, 2).Value = s.Dni;
                    worksheet.Cell(row, 3).Value = s.Celular;
                    worksheet.Cell(row, 4).Value = string.Join(", ", cuotas.Select(c => c.Mes));
                    worksheet.Cell(row, 5).Value = cuotas.Sum(c => c.Monto);
                    row++;
                }

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Morosos_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // GET: Socios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var socio = await _context.Socios
                .Include(s => s.Cuotas)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (socio == null) return NotFound();

            return View(socio);
        }

        // GET: Socios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Socios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Socio socio)
        {
            if (ModelState.IsValid)
            {
                _context.Add(socio);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(socio);
        }

        // GET: Socios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var socio = await _context.Socios.FindAsync(id);
            if (socio == null) return NotFound();
            
            return View(socio);
        }

        // POST: Socios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Socio socio)
        {
            if (id != socio.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(socio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SocioExists(socio.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(socio);
        }

        // GET: Socios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var socio = await _context.Socios
                .FirstOrDefaultAsync(m => m.Id == id);
            if (socio == null) return NotFound();

            return View(socio);
        }

        // POST: Socios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio != null)
            {
                _context.Socios.Remove(socio);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SocioExists(int id)
        {
            return _context.Socios.Any(e => e.Id == id);
        }
    }
}
