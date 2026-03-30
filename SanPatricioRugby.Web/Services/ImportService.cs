using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public class ImportService
    {
        private readonly ApplicationDbContext _context;

        public ImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResult();
            using var workbook = new XLWorkbook(fileStream);
            
            // 1. Validar Hoja Maestra (Flexible: que contenga LISTADO SOCIOS)
            var wsMaster = workbook.Worksheets.FirstOrDefault(w => w.Name.ToUpper().Contains("LISTADO SOCIOS"));
            if (wsMaster == null)
            {
                throw new Exception("El archivo no es compatible: no se encontró una hoja que contenga 'LISTADO SOCIOS'.");
            }

            // 2. Validar Encabezados de Hoja Maestra
            ValidateMasterHeaders(wsMaster);

            // 3. Obtener todos los socios actuales y marcarlos como inactivos "por defecto"
            var allSocios = await _context.Socios.ToListAsync();
            foreach (var s in allSocios) s.EsActivo = false;
            await _context.SaveChangesAsync();

            var dbSocios = allSocios.ToDictionary(s => NormalizeName(s.ApellidoNombre) + "|" + (s.Dni ?? ""), s => s);

            // 4. PROCESAR LISTA MAESTRA (SOCIOS ACTIVOS Y DATOS ACTUALES)
            await ProcessMasterSheet(wsMaster, dbSocios, result, true);

            // 5. PROCESAR HISTORIAL (POR SI HAY PAGOS ADICIONALES)
            var wsHistory = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "LISTADO SOCIOS 2025 JAVIER");
            if (wsHistory != null)
            {
                await ProcessMasterSheet(wsHistory, dbSocios, result, false);
            }

            // 6. PROCESAR DEBITOS
            var wsDebitos = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "MONICA DIAZ DEBITOS");
            if (wsDebitos != null)
            {
                await ProcessPaymentsSheet(wsDebitos, dbSocios, result, MedioPago.Debito);
            }

            // 7. PROCESAR TRANSFERENCIAS
            var wsTransferencias = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "MONICA DIAZ TRASFERENCIAS");
            if (wsTransferencias != null)
            {
                await ProcessPaymentsSheet(wsTransferencias, dbSocios, result, MedioPago.Transferencia);
            }

            await _context.SaveChangesAsync();
            return result;
        }

        private void ValidateMasterHeaders(IXLWorksheet ws)
        {
            var headers = new Dictionary<string, string>
            {
                { "A", "ID" },
                { "B", "TIPO SOCIO" },
                { "J", "APELLIDO Y NOMBRE" },
                { "N", "DNI" },
                { "L", "TARJETA" }
            };

            foreach (var h in headers)
            {
                var val = ws.Cell(1, h.Key).GetValue<string>()?.Trim().ToUpper();
                if (string.IsNullOrEmpty(val) || (!val.Contains(h.Value) && h.Value != "ID")) // ID can be # or Id
                {
                    if (h.Key == "A" && !string.IsNullOrEmpty(val)) continue; // Permitir cualquier ID en A1
                    throw new Exception($"El archivo no es compatible: falta el campo [{h.Value}] o el formato es incorrecto en la columna {h.Key}.");
                }
            }
        }

        private async Task ProcessMasterSheet(IXLWorksheet ws, Dictionary<string, Socio> dbSocios, ImportResult result, bool isActiveList)
        {
            var rows = ws.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                var nombreOriginal = row.Cell("J").GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(nombreOriginal) || nombreOriginal == "Apellido y Nombre") continue;

                var dni = row.Cell("N").GetValue<string>()?.Trim();
                var key = NormalizeName(nombreOriginal) + "|" + (dni ?? "");

                if (!dbSocios.TryGetValue(key, out var socio))
                {
                    // Reintentar solo por nombre si no hay DNI
                    var keyAlt = NormalizeName(nombreOriginal) + "|";
                    if (!dbSocios.TryGetValue(keyAlt, out socio))
                    {
                        socio = new Socio { ApellidoNombre = nombreOriginal, Dni = dni };
                        _context.Socios.Add(socio);
                        dbSocios[key] = socio;
                        result.SociosNuevos++;
                    }
                }

                if (isActiveList)
                {
                    socio.EsActivo = true;
                    socio.NumeroIdentificador = row.Cell("A").GetValue<string>();
                    socio.TipoSocio = row.Cell("B").GetValue<string>();
                    socio.Deporte = row.Cell("C").GetValue<string>();
                    socio.Division = row.Cell("D").GetValue<string>();
                    socio.Camada = row.Cell("E").GetValue<string>();
                    socio.Sexo = row.Cell("F").GetValue<string>();
                    socio.Celular = row.Cell("G").GetValue<string>();
                    socio.NumeroTarjeta = row.Cell("L").GetValue<string>();
                    socio.Acuerdos = row.Cell("L").GetValue<string>();
                    socio.FechaNacimiento2 = null;
                    var dateVal = row.Cell("I").GetValue<string>();
                    if (DateTime.TryParse(dateVal, out DateTime dt2)) socio.FechaNacimiento2 = dt2;

                    socio.NombreTitularTarjeta = row.Cell("M").GetValue<string>();
                    socio.MedioPagoPredeterminado = row.Cell("K").GetValue<string>().ToUpper().Contains("DEBITO") ? MedioPago.Debito : MedioPago.Transferencia;
                    if (socio.Id > 0) result.SociosActualizados++;
                }

                await _context.SaveChangesAsync();

                // Identificar si es BECADO
                bool isBecado = (socio.TipoSocio?.ToUpper().Contains("BECA") ?? false) || 
                                 (socio.Acuerdos?.ToUpper().Contains("BECA") ?? false);

                // Cuotas (O a AB)
                for (int col = 15; col <= 28; col++)
                {
                    var cellValue = row.Cell(col).GetValue<string>()?.Trim().ToUpper();
                    int mes = col <= 26 ? col - 14 : col - 26;
                    int anio = col <= 26 ? 2025 : 2026;

                    // Si es becado, forzamos estado PAGADO
                    if (isBecado && anio == 2025) cellValue = "PAGADO";

                    await UpdateCuotaStatus(socio, mes, anio, cellValue, socio.MedioPagoPredeterminado, result);
                }
            }
        }

        private async Task ProcessPaymentsSheet(IXLWorksheet ws, Dictionary<string, Socio> dbSocios, ImportResult result, MedioPago medio)
        {
            var rows = ws.RowsUsed().Skip(1);
            // Mapeo dinámico básico según sheet
            int colNombre = (medio == MedioPago.Debito) ? 3 : 1;
            int colDni = (medio == MedioPago.Debito) ? 2 : 3;
            int offsetMes = 4; 

            foreach (var row in rows)
            {
                var nombre = row.Cell(colNombre).GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(nombre) || nombre.Contains("NOMBRE") || nombre.Contains("TRASFERENCIA")) continue;

                var dni = row.Cell(colDni).GetValue<string>()?.Trim();
                var key = NormalizeName(nombre) + "|" + (dni ?? "");

                if (dbSocios.TryGetValue(key, out var socio))
                {
                    for (int mes = 1; mes <= 12; mes++)
                    {
                        var cellValue = row.Cell(offsetMes + mes - 1).GetValue<string>()?.Trim().ToUpper();
                        await UpdateCuotaStatus(socio, mes, 2025, cellValue, medio, result);
                    }
                }
            }
        }

        private async Task UpdateCuotaStatus(Socio socio, int mes, int anio, string? cellValue, MedioPago medio, ImportResult result)
        {
            if (string.IsNullOrEmpty(cellValue)) return;

            var cuota = await _context.Cuotas.FirstOrDefaultAsync(c => c.SocioId == socio.Id && c.Mes == mes && c.Anio == anio);
            bool exists = cuota != null;

            if (!exists)
            {
                cuota = new Cuota { SocioId = socio.Id, Mes = mes, Anio = anio, FechaVencimiento = new DateTime(anio, mes, 10), Monto = 50500 };
                _context.Cuotas.Add(cuota);
                result.CuotasCreadas++;
            }

            if (cellValue.Contains("PAGADO") || cellValue.Contains("PAGDO") || cellValue.Contains("PAGADFO") || cellValue.Contains("PAGAGO"))
            {
                if (cuota.Estado != EstadoPago.Pagado)
                {
                    cuota.Estado = EstadoPago.Pagado;
                    cuota.FechaPago = DateTime.Now;
                    cuota.MedioPagoUtilizado = medio;
                    if (exists) result.CuotasActualizadas++;
                }
            }
            else if (decimal.TryParse(cellValue, out decimal montoCerca))
            {
                cuota.Monto = montoCerca;
            }
        }

        private string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Trim().ToUpper().Replace("  ", " ");
        }
    }

    public class ImportResult
    {
        public int SociosNuevos { get; set; }
        public int SociosActualizados { get; set; }
        public int CuotasCreadas { get; set; }
        public int CuotasActualizadas { get; set; }
    }
}
