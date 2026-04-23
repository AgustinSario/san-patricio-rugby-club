using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using SanPatricioRugby.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using SanPatricioRugby.Web.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SanPatricioRugby.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SociosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICarnetService _carnetService;
        private readonly IWebHostEnvironment _env;

        public SociosController(ApplicationDbContext context, ICarnetService carnetService, IWebHostEnvironment env)
        {
            _context = context;
            _carnetService = carnetService;
            _env = env;
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
                .Include(s => s.GrupoFamiliar)
                    .ThenInclude(g => g!.Miembros)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (socio == null) return NotFound();

            ViewBag.GruposFamiliares = await _context.GruposFamiliares.OrderBy(g => g.Nombre).ToListAsync();

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
        public async Task<IActionResult> Create([Bind("NumeroIdentificador,ApellidoNombre,Dni,FechaNacimiento,Sexo,Celular,TipoSocio,Deporte,Division,Camada,MedioPagoPredeterminado,NumeroTarjeta,NombreTitularTarjeta,Acuerdos,FechaNacimiento2,EsActivo,Email")] Socio socio, IFormFile? FotoUpload)
        {
            if (ModelState.IsValid)
            {
                // Handle photo upload
                if (FotoUpload != null && FotoUpload.Length > 0)
                {
                    var photoPath = await SavePhotoAsync(FotoUpload, socio.Id);
                    socio.FotoPath = photoPath;
                }

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
            
            ViewBag.GruposFamiliares = await _context.GruposFamiliares.OrderBy(g => g.Nombre).ToListAsync();

            return View(socio);
        }

        // POST: Socios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NumeroIdentificador,ApellidoNombre,Dni,FechaNacimiento,Sexo,Celular,TipoSocio,Deporte,Division,Camada,MedioPagoPredeterminado,NumeroTarjeta,NombreTitularTarjeta,Acuerdos,FechaNacimiento2,EsActivo,Email")] Socio socio, IFormFile? FotoUpload, bool RemovePhoto = false)
        {
            if (id != socio.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSocio = await _context.Socios.FindAsync(id);
                    if (existingSocio == null) return NotFound();

                    // Handle photo removal
                    if (RemovePhoto && !string.IsNullOrEmpty(existingSocio.FotoPath))
                    {
                        var oldPhotoPath = Path.Combine(_env.ContentRootPath, "wwwroot", existingSocio.FotoPath);
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                        existingSocio.FotoPath = null;
                    }

                    // Handle new photo upload
                    if (FotoUpload != null && FotoUpload.Length > 0)
                    {
                        // Delete old photo if exists
                        if (!string.IsNullOrEmpty(existingSocio.FotoPath))
                        {
                            var oldPhotoPath = Path.Combine(_env.ContentRootPath, "wwwroot", existingSocio.FotoPath);
                            if (System.IO.File.Exists(oldPhotoPath))
                            {
                                System.IO.File.Delete(oldPhotoPath);
                            }
                        }

                        var photoPath = await SavePhotoAsync(FotoUpload, socio.Id);
                        existingSocio.FotoPath = photoPath;
                    }

                    // Update other fields
                    existingSocio.NumeroIdentificador = socio.NumeroIdentificador;
                    existingSocio.ApellidoNombre = socio.ApellidoNombre;
                    existingSocio.Dni = socio.Dni;
                    existingSocio.FechaNacimiento = socio.FechaNacimiento;
                    existingSocio.Sexo = socio.Sexo;
                    existingSocio.Celular = socio.Celular;
                    existingSocio.TipoSocio = socio.TipoSocio;
                    existingSocio.Deporte = socio.Deporte;
                    existingSocio.Division = socio.Division;
                    existingSocio.Camada = socio.Camada;
                    existingSocio.MedioPagoPredeterminado = socio.MedioPagoPredeterminado;
                    existingSocio.NumeroTarjeta = socio.NumeroTarjeta;
                    existingSocio.NombreTitularTarjeta = socio.NombreTitularTarjeta;
                    existingSocio.Acuerdos = socio.Acuerdos;
                    existingSocio.FechaNacimiento2 = socio.FechaNacimiento2;
                    existingSocio.EsActivo = socio.EsActivo;
                    existingSocio.Email = socio.Email;

                    _context.Update(existingSocio);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CobrarCuota(int id)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Socio)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cuota == null) return NotFound();

            // 1. Marcar como pagada
            cuota.Estado = EstadoPago.Pagado;
            cuota.FechaPago = DateTime.Now;
            
            // Asignar medio de pago si el socio lo tiene (si no, queda el anterior)
            if (cuota.Socio != null && cuota.Socio.MedioPagoPredeterminado != MedioPago.NoRegistrado)
            {
                cuota.MedioPagoUtilizado = cuota.Socio.MedioPagoPredeterminado;
            }

            // 2. Buscar si tiene cuotas anteriores sin pagar para avisar
            var deudasAnteriores = await _context.Cuotas
                .Where(c => c.SocioId == cuota.SocioId 
                            && c.Id != cuota.Id 
                            && c.Estado != EstadoPago.Pagado 
                            && (c.Anio < cuota.Anio || (c.Anio == cuota.Anio && c.Mes < cuota.Mes)))
                .OrderBy(c => c.Anio).ThenBy(c => c.Mes)
                .ToListAsync();

            if (deudasAnteriores.Any())
            {
                var mesesDeuda = string.Join(", ", deudasAnteriores.Select(c => $"{c.Mes}/{c.Anio}"));
                TempData["Warning"] = $"Se registró el pago de {cuota.Mes}/{cuota.Anio}. ¡AVISO! El socio aún debe periodos ANTERIORES: {mesesDeuda}.";
            }
            else
            {
                TempData["Success"] = $"El pago de {cuota.Mes}/{cuota.Anio} se registró con éxito.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = cuota.SocioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarPeriodo(int id)
        {
            var socio = await _context.Socios
                .Include(s => s.Cuotas)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (socio == null) return NotFound();

            // Buscar la última cuota registrada para saber qué mes sigue
            var ultimaCuota = socio.Cuotas
                .OrderByDescending(c => c.Anio)
                .ThenByDescending(c => c.Mes)
                .FirstOrDefault();

            int nuevoMes, nuevoAnio;
            decimal monto = 50500; // Monto por defecto si no hay cuotas previas

            if (ultimaCuota != null)
            {
                nuevoMes = ultimaCuota.Mes + 1;
                nuevoAnio = ultimaCuota.Anio;
                monto = ultimaCuota.Monto;

                if (nuevoMes > 12)
                {
                    nuevoMes = 1;
                    nuevoAnio++;
                }
            }
            else
            {
                nuevoMes = DateTime.Now.Month;
                nuevoAnio = DateTime.Now.Year;
            }

            // Validar si ya existe ese periodo para no duplicar
            if (socio.Cuotas.Any(c => c.Mes == nuevoMes && c.Anio == nuevoAnio))
            {
                TempData["Warning"] = $"El periodo {nuevoMes}/{nuevoAnio} ya existe para este socio.";
                return RedirectToAction(nameof(Details), new { id = socio.Id });
            }

            var nuevaCuota = new Cuota
            {
                SocioId = socio.Id,
                Mes = nuevoMes,
                Anio = nuevoAnio,
                Monto = monto,
                Estado = EstadoPago.Pendiente,
                FechaVencimiento = new DateTime(nuevoAnio, nuevoMes, 10)
            };

            _context.Cuotas.Add(nuevaCuota);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Periodo {nuevoMes}/{nuevoAnio} generado correctamente.";
            return RedirectToAction(nameof(Details), new { id = socio.Id });
        }

        // GET: Socios/Becados
        public async Task<IActionResult> Becados(string searchString, string searchDni, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDni"] = searchDni;

            var query = _context.Socios
                .Where(s => s.EsActivo && 
                            ((s.TipoSocio != null && s.TipoSocio.ToUpper().Contains("BECA")) || 
                             (s.Acuerdos != null && s.Acuerdos.ToUpper().Contains("BECA"))));

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
            return View(await PaginatedList<Socio>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> ExportarBecadosPdf()
        {
            var becados = await _context.Socios
                .Where(s => s.EsActivo && 
                            ((s.TipoSocio != null && s.TipoSocio.ToUpper().Contains("BECA")) || 
                             (s.Acuerdos != null && s.Acuerdos.ToUpper().Contains("BECA"))))
                .OrderBy(s => s.ApellidoNombre)
                .ToListAsync();

            var data = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(QuestPDF.Helpers.Fonts.Arial));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SAN PATRICIO RUGBY CLUB").FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Red.Medium);
                            col.Item().Text("Listado de Socios Becados").FontSize(14).Medium();
                        });

                        row.ConstantItem(100).AlignRight().Text($"{DateTime.Now:dd/MM/yyyy}").FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                    page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Socio");
                                header.Cell().Element(CellStyle).Text("DNI");
                                header.Cell().Element(CellStyle).Text("Tipo");
                                header.Cell().Element(CellStyle).Text("Acuerdos");

                                QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
                            });

                            int i = 1;
                            foreach (var socio in becados)
                            {
                                table.Cell().Element(CellStyle).Text(i++.ToString());
                                table.Cell().Element(CellStyle).Text(socio.ApellidoNombre);
                                table.Cell().Element(CellStyle).Text(socio.Dni ?? "-");
                                table.Cell().Element(CellStyle).Text(socio.TipoSocio ?? "-");
                                table.Cell().Element(CellStyle).Text(socio.Acuerdos ?? "-");

                                QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            using (var stream = new System.IO.MemoryStream())
            {
                data.GeneratePdf(stream);
                return File(stream.ToArray(), "application/pdf", $"Becados_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarCarnet(int id)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio == null) return NotFound();

            if (string.IsNullOrEmpty(socio.Dni))
            {
                TempData["Warning"] = "No se puede generar el carnet porque falta el DNI del socio.";
                return RedirectToAction(nameof(Details), new { id = socio.Id });
            }

            try
            {
                var relativePath = await _carnetService.GenerarCarnetImagenAsync(socio, _env.ContentRootPath);
                socio.CarnetPath = relativePath;
                _context.Update(socio);
                await _context.SaveChangesAsync();

                TempData["Success"] = "¡Carnet generado con éxito!";
            }
            catch (Exception ex)
            {
                TempData["Warning"] = "Error al generar el carnet: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = socio.Id });
        }

        public async Task<IActionResult> DescargarCarnetPdf(int id)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio == null) return NotFound();

            if (string.IsNullOrEmpty(socio.Dni))
            {
                return BadRequest("El socio no tiene DNI cargado.");
            }

            try
            {
                var pdfBytes = await _carnetService.GenerarCarnetPdfAsync(socio, _env.ContentRootPath);
                return File(pdfBytes, "application/pdf", $"Carnet_{socio.ApellidoNombre.Replace(" ", "_")}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al generar el PDF: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarTodosLosCarnets()
        {
            var socios = await _context.Socios
                .Where(s => s.EsActivo && !string.IsNullOrEmpty(s.Dni))
                .ToListAsync();

            int generados = 0;
            int errores = 0;

            foreach (var socio in socios)
            {
                try
                {
                    var relativePath = await _carnetService.GenerarCarnetImagenAsync(socio, _env.ContentRootPath);
                    socio.CarnetPath = relativePath;
                    generados++;
                }
                catch
                {
                    errores++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Proceso finalizado. Carnets generados: {generados}. Errores: {errores}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearGrupoFamiliar(string nombre, int socioId)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Warning"] = "El nombre del grupo no puede estar vacío.";
                return RedirectToAction(nameof(Details), new { id = socioId });
            }

            var grupo = new GrupoFamiliar { Nombre = nombre };
            _context.GruposFamiliares.Add(grupo);
            await _context.SaveChangesAsync();

            var socio = await _context.Socios.FindAsync(socioId);
            if (socio != null)
            {
                socio.GrupoFamiliarId = grupo.Id;
                socio.EsTitularGrupoFamiliar = true; // Por defecto el que lo crea es el titular
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Grupo '{nombre}' creado y socio asignado como titular.";
            return RedirectToAction(nameof(Details), new { id = socioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarGrupoFamiliar(int socioId, int grupoId)
        {
            var socio = await _context.Socios.FindAsync(socioId);
            if (socio == null) return NotFound();

            socio.GrupoFamiliarId = grupoId;
            socio.EsTitularGrupoFamiliar = false; // Al unirse a uno existente, entra como miembro
            await _context.SaveChangesAsync();

            TempData["Success"] = "Socio asignado al grupo familiar correctamente.";
            return RedirectToAction(nameof(Details), new { id = socioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarDeGrupoFamiliar(int socioId)
        {
            var socio = await _context.Socios.FindAsync(socioId);
            if (socio == null) return NotFound();

            socio.GrupoFamiliarId = null;
            socio.EsTitularGrupoFamiliar = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Socio quitado del grupo familiar.";
            return RedirectToAction(nameof(Details), new { id = socioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTitularGrupoFamiliar(int socioId)
        {
            var socio = await _context.Socios.FindAsync(socioId);
            if (socio == null || !socio.GrupoFamiliarId.HasValue) return NotFound();

            // Quitar titularidad a otros del mismo grupo
            var otrosMiembros = await _context.Socios
                .Where(s => s.GrupoFamiliarId == socio.GrupoFamiliarId && s.Id != socio.Id)
                .ToListAsync();
            
            foreach (var s in otrosMiembros)
            {
                s.EsTitularGrupoFamiliar = false;
            }

            socio.EsTitularGrupoFamiliar = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "El socio ahora es el titular del grupo familiar.";
            return RedirectToAction(nameof(Details), new { id = socioId });
        }

        private async Task<string> SavePhotoAsync(IFormFile foto, int socioId)
        {
            var fileName = $"foto_{socioId}.jpg";
            var relativePath = Path.Combine("images", "fotos", fileName);
            var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", relativePath);

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            using (var image = SixLabors.ImageSharp.Image.Load(foto.OpenReadStream()))
            {
                // Resize to max 800x800 to save space (medium quality)
                image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(800, 800),
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                }));

                // Compress with quality 75 to save space
                image.Save(fullPath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 75 });
            }

            return relativePath;
        }
    }
}
