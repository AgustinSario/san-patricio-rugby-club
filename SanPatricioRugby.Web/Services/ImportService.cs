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
            
            // Hoja Maestra identificada
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "LISTADO SOCIOS 2025 JAVIER ");
            if (ws == null) throw new Exception("No se encontró la hoja 'LISTADO SOCIOS 2025 JAVIER '");

            var dbSocios = await _context.Socios.ToDictionaryAsync(s => NormalizeName(s.ApellidoNombre) + "|" + (s.Dni ?? ""), s => s);
            var rows = ws.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var nombreOriginal = row.Cell("J").GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(nombreOriginal) || nombreOriginal == "Apellido y Nombre") continue;

                var dni = row.Cell("N").GetValue<string>()?.Trim();
                var key = NormalizeName(nombreOriginal) + "|" + (dni ?? "");

                if (!dbSocios.TryGetValue(key, out var socio))
                {
                    // Intentar solo por nombre si el DNI es nulo
                    key = NormalizeName(nombreOriginal) + "|";
                    if (!dbSocios.TryGetValue(key, out socio))
                    {
                        socio = new Socio { ApellidoNombre = nombreOriginal, Dni = dni, EsActivo = true };
                        _context.Socios.Add(socio);
                        result.SociosNuevos++;
                    }
                    else
                    {
                        result.SociosActualizados++;
                    }
                }
                else
                {
                    result.SociosActualizados++;
                }

                // Actualizar datos del Socio desde el Excel (lo más reciente manda)
                socio.NumeroIdentificador = row.Cell("A").GetValue<string>();
                socio.TipoSocio = row.Cell("B").GetValue<string>();
                socio.Deporte = row.Cell("C").GetValue<string>();
                socio.Division = row.Cell("D").GetValue<string>();
                socio.Camada = row.Cell("E").GetValue<string>();
                socio.Sexo = row.Cell("F").GetValue<string>();
                socio.Celular = row.Cell("G").GetValue<string>();
                socio.NumeroTarjeta = row.Cell("L").GetValue<string>();
                socio.NombreTitularTarjeta = row.Cell("M").GetValue<string>();
                socio.MedioPagoPredeterminado = row.Cell("K").GetValue<string>().ToUpper().Contains("DEBITO") ? MedioPago.Debito : MedioPago.Transferencia;

                await _context.SaveChangesAsync(); // Asegurar ID para cuotas

                // Procesar Cuotas (15 a 28)
                for (int col = 15; col <= 28; col++)
                {
                    var cellValue = row.Cell(col).GetValue<string>()?.Trim().ToUpper();
                    int mes = col <= 26 ? col - 14 : col - 26;
                    int anio = col <= 26 ? 2025 : 2026;

                    var cuota = await _context.Cuotas.FirstOrDefaultAsync(c => c.SocioId == socio.Id && c.Mes == mes && c.Anio == anio);
                    bool exists = cuota != null;

                    if (!exists)
                    {
                        cuota = new Cuota { SocioId = socio.Id, Mes = mes, Anio = anio, FechaVencimiento = new DateTime(anio, mes, 10), Monto = 50500 };
                        _context.Cuotas.Add(cuota);
                        result.CuotasCreadas++;
                    }

                    // Lógica de actualización de estado
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        if (cellValue.Contains("PAGADO") || cellValue.Contains("PAGDO") || cellValue.Contains("PAGADFO") || cellValue.Contains("PAGAGO"))
                        {
                            if (cuota.Estado != EstadoPago.Pagado)
                            {
                                cuota.Estado = EstadoPago.Pagado;
                                cuota.FechaPago = DateTime.Now;
                                cuota.MedioPagoUtilizado = socio.MedioPagoPredeterminado;
                                if (!exists) { /* ya contada */ } else result.CuotasActualizadas++;
                            }
                        }
                        else if (decimal.TryParse(cellValue, out decimal montoCerca))
                        {
                            if (cuota.Estado != EstadoPago.Pagado)
                            {
                                cuota.Estado = (anio < DateTime.Now.Year || (anio == DateTime.Now.Year && mes < DateTime.Now.Month)) ? EstadoPago.Vencido : EstadoPago.Pendiente;
                                cuota.Monto = montoCerca;
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return result;
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
